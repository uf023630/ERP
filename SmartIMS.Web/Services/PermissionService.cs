using Microsoft.Data.SqlClient;
using SmartIMS.Web.Infrastructure;
using SmartIMS.Web.Models;

namespace SmartIMS.Web.Services;

public sealed class PermissionService
{
    private readonly SqlConnectionFactory _connectionFactory;

    public PermissionService(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public static IReadOnlyList<MenuGroupDefinition> MenuGroups { get; } =
    [
        new("系統管理",
        [
            new("使用者管理", "Admin", "Users", "PAGE_ADMIN_USERS"),
            new("權限設定", "Admin", "Permissions", "PAGE_ADMIN_PERMISSIONS"),
            new("公司形象", "Admin", "Branding", "PAGE_ADMIN_BRANDING")
        ]),
        new("基礎資料",
        [
            new("基礎資料預留", "Home", "Index", "PAGE_DASHBOARD"),
            new("元件測試頁面", "Home", "ComponentTest", "PAGE_COMPONENT_TEST"),
            new("客戶資料管理", "Home", "CustomerLayoutTest", "PAGE_COMPONENT_TEST"),
            new("商品資料管理", "Home", "ProductLayoutTest", "PAGE_COMPONENT_TEST")
        ])
    ];

    public async Task<IReadOnlyList<string>> GetUserPermissionCodesAsync(long appUserId)
    {
        var permissions = new List<string>();
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.PermissionCode
            FROM dbo.UserPermission up
            INNER JOIN dbo.AppPermission p ON p.PermissionID = up.PermissionID AND p.IsActive = 1
            WHERE up.AppUserID = @AppUserID
            ORDER BY p.PermissionCode;
            """;
        command.Parameters.AddWithValue("@AppUserID", appUserId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            permissions.Add(reader.GetString(0));
        }

        return permissions;
    }

    public async Task<IReadOnlyList<PermissionDefinition>> GetAllPermissionsAsync()
    {
        var permissions = new List<PermissionDefinition>();
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT PermissionID, PermissionCode, PermissionName, ModuleCode, ActionCode, Description, IsActive
            FROM dbo.AppPermission
            ORDER BY CASE ModuleCode WHEN N'PAGE' THEN 0 WHEN N'ADMIN' THEN 1 ELSE 2 END, PermissionCode;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            permissions.Add(new PermissionDefinition(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetBoolean(6)));
        }

        return permissions;
    }

    public async Task<HashSet<long>> GetUserPermissionIdsAsync(long appUserId)
    {
        var permissionIds = new HashSet<long>();
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT PermissionID FROM dbo.UserPermission WHERE AppUserID = @AppUserID;";
        command.Parameters.AddWithValue("@AppUserID", appUserId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            permissionIds.Add(reader.GetInt64(0));
        }

        return permissionIds;
    }

    public async Task ReplaceUserPermissionsAsync(long appUserId, IEnumerable<long> permissionIds)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        await using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM dbo.UserPermission WHERE AppUserID = @AppUserID;";
            deleteCommand.Parameters.AddWithValue("@AppUserID", appUserId);
            await deleteCommand.ExecuteNonQueryAsync();
        }

        foreach (var permissionId in permissionIds.Distinct())
        {
            await using var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = """
                IF EXISTS (SELECT 1 FROM dbo.AppPermission WHERE PermissionID = @PermissionID)
                BEGIN
                    INSERT INTO dbo.UserPermission (AppUserID, PermissionID)
                    VALUES (@AppUserID, @PermissionID);
                END;
                """;
            insertCommand.Parameters.AddWithValue("@AppUserID", appUserId);
            insertCommand.Parameters.AddWithValue("@PermissionID", permissionId);
            await insertCommand.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }
}
