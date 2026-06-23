using Microsoft.Data.SqlClient;
using SmartIMS.Web.Infrastructure;
using SmartIMS.Web.Models;

namespace SmartIMS.Web.Services;

public sealed class SystemSettingsService
{
    private readonly SqlConnectionFactory _connectionFactory;

    public SystemSettingsService(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Dictionary<string, string>> GetSettingsAsync()
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT SettingKey, SettingValue FROM dbo.SystemSetting WHERE IsActive = 1;";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            settings[reader.GetString(0)] = reader.IsDBNull(1) ? "" : reader.GetString(1);
        }

        return settings;
    }

    public async Task<SystemBranding> GetBrandingAsync()
    {
        var settings = await GetSettingsAsync();
        return new SystemBranding(
            GetSetting(settings, "COMPANY_NAME", "智慧進銷存系統"),
            GetNullableSetting(settings, "COMPANY_LOGO_PATH"),
            GetNullableSetting(settings, "LOGIN_HERO_IMAGE_PATH"),
            GetSetting(settings, "LOGIN_TITLE", "智慧進銷存系統"),
            GetSetting(settings, "LOGIN_SUBTITLE", "請使用管理員提供的帳號登入"));
    }

    public async Task<SystemSkin> GetSkinAsync()
    {
        var settings = await GetSettingsAsync();
        return SystemSkin.Create(GetSetting(settings, "SYSTEM_SKIN", SystemSkin.DefaultSkinKey));
    }

    public async Task SaveBrandingAsync(SystemBranding branding, SystemSkin skin)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        await UpsertSettingAsync(connection, transaction, "COMPANY_NAME", "公司名稱", "公司形象", branding.CompanyName, "顯示於 Header 與系統標題");
        await UpsertSettingAsync(connection, transaction, "COMPANY_LOGO_PATH", "Company Logo", "Branding", branding.CompanyLogoPath ?? "", "Logo used in the header and login card");
        await UpsertSettingAsync(connection, transaction, "LOGIN_HERO_IMAGE_PATH", "Login Hero Image", "Branding", branding.LoginHeroImagePath ?? "", "Hero background image for the login page");
        await UpsertSettingAsync(connection, transaction, "LOGIN_TITLE", "登入標題", "公司形象", branding.LoginTitle, "登入頁主標題");
        await UpsertSettingAsync(connection, transaction, "LOGIN_SUBTITLE", "登入副標題", "公司形象", branding.LoginSubtitle, "登入頁副標題");
        await UpsertSettingAsync(connection, transaction, "SYSTEM_SKIN", "系統皮膚", "介面設定", skin.SkinKey, "全站套用的介面皮膚");

        await transaction.CommitAsync();
    }

    private static async Task UpsertSettingAsync(SqlConnection connection, SqlTransaction transaction, string key, string name, string category, string value, string description)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            MERGE dbo.SystemSetting AS target
            USING (SELECT @SettingKey AS SettingKey, @SettingName AS SettingName, @Category AS Category, @SettingValue AS SettingValue, @Description AS Description) AS source
            ON target.SettingKey = source.SettingKey
            WHEN MATCHED THEN
              UPDATE SET SettingName = source.SettingName, Category = source.Category, SettingValue = source.SettingValue, Description = source.Description, IsActive = 1, UpdatedAt = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN
              INSERT (SettingKey, SettingName, Category, SettingValue, Description, IsActive)
              VALUES (source.SettingKey, source.SettingName, source.Category, source.SettingValue, source.Description, 1);
            """;
        command.Parameters.AddWithValue("@SettingKey", key);
        command.Parameters.AddWithValue("@SettingName", name);
        command.Parameters.AddWithValue("@Category", category);
        command.Parameters.AddWithValue("@SettingValue", value);
        command.Parameters.AddWithValue("@Description", description);
        await command.ExecuteNonQueryAsync();
    }

    private static string GetSetting(Dictionary<string, string> settings, string key, string fallback)
    {
        return settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    private static string? GetNullableSetting(Dictionary<string, string> settings, string key)
    {
        return settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;
    }
}
