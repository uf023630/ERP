using System.Text.Json;
using Microsoft.Data.SqlClient;
using SmartIMS.Web.Infrastructure;
using SmartIMS.Web.Models;

namespace SmartIMS.Web.Services;

public sealed class ListViewSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SqlConnectionFactory _connectionFactory;

    public ListViewSettingsService(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task ApplySettingsAsync(long appUserId, ListViewModel listView)
    {
        var savedSettings = await GetSettingsAsync(appUserId, listView.ListKey);
        var savedByKey = savedSettings?.Columns.ToDictionary(column => column.Key, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, ListViewColumnSetting>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < listView.Columns.Count; index++)
        {
            var column = listView.Columns[index];
            column.Order = index;
            column.Visible = column.DefaultVisible;
            column.Width = column.DefaultWidth;

            if (!savedByKey.TryGetValue(column.Key, out var savedColumn))
            {
                continue;
            }

            column.Visible = savedColumn.Visible;
            column.Width = Math.Max(column.MinWidth, savedColumn.Width);
            column.Order = Math.Max(0, savedColumn.Order);
        }
    }

    public async Task SaveSettingsAsync(long appUserId, string listKey, IEnumerable<string> allowedColumnKeys, IEnumerable<ListViewColumnSetting> columns)
    {
        var allowed = allowedColumnKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(listKey) || allowed.Count == 0)
        {
            throw new InvalidOperationException("ListView setting target is invalid.");
        }

        var normalizedColumns = columns
            .Where(column => allowed.Contains(column.Key))
            .GroupBy(column => column.Key, StringComparer.OrdinalIgnoreCase)
            .Select((group, index) =>
            {
                var column = group.First();
                return new ListViewColumnSetting
                {
                    Key = column.Key,
                    Visible = column.Visible,
                    Width = Math.Clamp(column.Width, 80, 1200),
                    Order = Math.Max(0, column.Order == 0 && index > 0 ? index : column.Order)
                };
            })
            .OrderBy(column => column.Order)
            .ToList();

        var settings = new ListViewSettingsDocument { Columns = normalizedColumns };
        var settingsJson = JsonSerializer.Serialize(settings, JsonOptions);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            MERGE dbo.UserListViewSetting AS target
            USING (SELECT @AppUserID AS AppUserID, @ListKey AS ListKey) AS source
            ON target.AppUserID = source.AppUserID AND target.ListKey = source.ListKey
            WHEN MATCHED THEN
              UPDATE SET SettingsJson = @SettingsJson, UpdatedAt = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN
              INSERT (AppUserID, ListKey, SettingsJson, UpdatedAt)
              VALUES (@AppUserID, @ListKey, @SettingsJson, SYSUTCDATETIME());
            """;
        command.Parameters.AddWithValue("@AppUserID", appUserId);
        command.Parameters.AddWithValue("@ListKey", listKey);
        command.Parameters.AddWithValue("@SettingsJson", settingsJson);
        await command.ExecuteNonQueryAsync();
    }

    public async Task ResetSettingsAsync(long appUserId, string listKey)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM dbo.UserListViewSetting WHERE AppUserID = @AppUserID AND ListKey = @ListKey;";
        command.Parameters.AddWithValue("@AppUserID", appUserId);
        command.Parameters.AddWithValue("@ListKey", listKey);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<ListViewSettingsDocument?> GetSettingsAsync(long appUserId, string listKey)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT SettingsJson
            FROM dbo.UserListViewSetting
            WHERE AppUserID = @AppUserID AND ListKey = @ListKey;
            """;
        command.Parameters.AddWithValue("@AppUserID", appUserId);
        command.Parameters.AddWithValue("@ListKey", listKey);

        var value = await command.ExecuteScalarAsync();
        if (value is not string settingsJson || string.IsNullOrWhiteSpace(settingsJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ListViewSettingsDocument>(settingsJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
