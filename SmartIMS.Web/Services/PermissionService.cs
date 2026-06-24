using Microsoft.Data.SqlClient;
using SmartIMS.Web.Infrastructure;
using SmartIMS.Web.Models;
using SmartIMS.Web.ViewModels;

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
            new("客戶資料管理", "Home", "CustomerLayoutTest", "PAGE_CUSTOMER_LAYOUT_TEST"),
            new("商品資料管理", "Home", "ProductLayoutTest", "PAGE_PRODUCT_LAYOUT_TEST"),
            new("商品材質管理", "Home", "MaterialLayoutTest", "PAGE_PRODUCT_MATERIAL"),
            new("供應商管理", "Home", "SupplierLayoutTest", "PAGE_SUPPLIER")
        ])
    ];

    public async Task<IReadOnlyList<string>> GetUserPermissionCodesAsync(long appUserId)
    {
        var permissions = new List<string>();
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT PermissionCode
            FROM (
                SELECT pa.PermissionCode
                FROM dbo.UserRole ur
                INNER JOIN dbo.AppRole r ON r.RoleID = ur.RoleID AND r.IsActive = 1
                INNER JOIN dbo.RolePageAction rpa ON rpa.RoleID = r.RoleID
                INNER JOIN dbo.PageAction pa ON pa.PageActionID = rpa.PageActionID AND pa.IsActive = 1
                INNER JOIN dbo.AppPage pg ON pg.PageID = pa.PageID AND pg.IsActive = 1
                INNER JOIN dbo.AppAction a ON a.ActionID = pa.ActionID AND a.IsActive = 1
                WHERE ur.AppUserID = @AppUserID

                UNION

                SELECT p.PermissionCode
                FROM dbo.UserPermission up
                INNER JOIN dbo.AppPermission p ON p.PermissionID = up.PermissionID AND p.IsActive = 1
                WHERE up.AppUserID = @AppUserID
            ) permissions
            ORDER BY PermissionCode;
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

    public async Task<IReadOnlyList<RoleOptionViewModel>> GetRolesAsync()
    {
        var roles = new List<RoleOptionViewModel>();
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT RoleID, RoleCode, RoleName, Description, IsSystemRole, IsActive
            FROM dbo.AppRole
            ORDER BY IsSystemRole DESC, RoleName;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            roles.Add(new RoleOptionViewModel(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetBoolean(4),
                reader.GetBoolean(5)));
        }

        return roles;
    }

    public async Task<HashSet<long>> GetUserRoleIdsAsync(long appUserId)
    {
        var roleIds = new HashSet<long>();
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT RoleID FROM dbo.UserRole WHERE AppUserID = @AppUserID;";
        command.Parameters.AddWithValue("@AppUserID", appUserId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            roleIds.Add(reader.GetInt64(0));
        }

        return roleIds;
    }

    public async Task ReplaceUserRolesAsync(long appUserId, IEnumerable<long> roleIds)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        await using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM dbo.UserRole WHERE AppUserID = @AppUserID;";
            deleteCommand.Parameters.AddWithValue("@AppUserID", appUserId);
            await deleteCommand.ExecuteNonQueryAsync();
        }

        foreach (var roleId in roleIds.Distinct())
        {
            await using var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = """
                IF EXISTS (SELECT 1 FROM dbo.AppRole WHERE RoleID = @RoleID AND IsActive = 1)
                BEGIN
                    INSERT INTO dbo.UserRole (AppUserID, RoleID)
                    VALUES (@AppUserID, @RoleID);
                END;
                """;
            insertCommand.Parameters.AddWithValue("@AppUserID", appUserId);
            insertCommand.Parameters.AddWithValue("@RoleID", roleId);
            await insertCommand.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    public async Task<RolePermissionEditViewModel> GetRolePermissionEditModelAsync(long? roleId)
    {
        var roles = await GetRolesAsync();
        var selectedRole = roleId.HasValue
            ? roles.FirstOrDefault(role => role.RoleId == roleId.Value)
            : roles.FirstOrDefault();

        var selectedPageActionIds = selectedRole is null
            ? new HashSet<long>()
            : await GetRolePageActionIdsAsync(selectedRole.RoleId);

        return new RolePermissionEditViewModel
        {
            SelectedRoleId = selectedRole?.RoleId,
            SelectedRoleName = selectedRole?.RoleName ?? "",
            Roles = roles,
            Pages = await GetRolePermissionPagesAsync(),
            SelectedPageActionIds = selectedPageActionIds
        };
    }

    public async Task ReplaceRolePermissionsAsync(long roleId, IEnumerable<long> pageActionIds)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

        await using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM dbo.RolePageAction WHERE RoleID = @RoleID;";
            deleteCommand.Parameters.AddWithValue("@RoleID", roleId);
            await deleteCommand.ExecuteNonQueryAsync();
        }

        foreach (var pageActionId in pageActionIds.Distinct())
        {
            await using var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = transaction;
            insertCommand.CommandText = """
                IF EXISTS (SELECT 1 FROM dbo.AppRole WHERE RoleID = @RoleID AND IsActive = 1)
                   AND EXISTS (SELECT 1 FROM dbo.PageAction WHERE PageActionID = @PageActionID AND IsActive = 1)
                BEGIN
                    INSERT INTO dbo.RolePageAction (RoleID, PageActionID)
                    VALUES (@RoleID, @PageActionID);
                END;
                """;
            insertCommand.Parameters.AddWithValue("@RoleID", roleId);
            insertCommand.Parameters.AddWithValue("@PageActionID", pageActionId);
            await insertCommand.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    private async Task<HashSet<long>> GetRolePageActionIdsAsync(long roleId)
    {
        var pageActionIds = new HashSet<long>();
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT PageActionID FROM dbo.RolePageAction WHERE RoleID = @RoleID;";
        command.Parameters.AddWithValue("@RoleID", roleId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            pageActionIds.Add(reader.GetInt64(0));
        }

        return pageActionIds;
    }

    private async Task<IReadOnlyList<RolePermissionPageViewModel>> GetRolePermissionPagesAsync()
    {
        var pages = new List<RolePermissionPageBuilder>();
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT pg.PageID, pg.PageCode, pg.PageName, pg.MenuGroup,
                   pa.PageActionID, a.ActionCode, a.ActionName, pa.PermissionCode
            FROM dbo.AppPage pg
            INNER JOIN dbo.PageAction pa ON pa.PageID = pg.PageID AND pa.IsActive = 1
            INNER JOIN dbo.AppAction a ON a.ActionID = pa.ActionID AND a.IsActive = 1
            WHERE pg.IsActive = 1
            ORDER BY pg.MenuSortOrder, pg.SortOrder, pg.PageName, a.SortOrder, a.ActionName;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var pageId = reader.GetInt64(0);
            var page = pages.FirstOrDefault(item => item.PageId == pageId);
            if (page is null)
            {
                page = new RolePermissionPageBuilder(
                    pageId,
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetString(3));
                pages.Add(page);
            }

            page.Actions.Add(new RolePermissionActionViewModel(
                reader.GetInt64(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7)));
        }

        return pages
            .Select(page => new RolePermissionPageViewModel(
                page.PageId,
                page.PageCode,
                page.PageName,
                page.MenuGroup,
                page.Actions))
            .ToList();
    }

    private sealed record RolePermissionPageBuilder(
        long PageId,
        string PageCode,
        string PageName,
        string MenuGroup)
    {
        public List<RolePermissionActionViewModel> Actions { get; } = [];
    }
}
