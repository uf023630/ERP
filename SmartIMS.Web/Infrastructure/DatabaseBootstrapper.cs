using Microsoft.Data.SqlClient;
using SmartIMS.Web.Security;

namespace SmartIMS.Web.Infrastructure;

public sealed class DatabaseBootstrapper
{
    private readonly SqlConnectionFactory _connectionFactory;

    public DatabaseBootstrapper(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task EnsureAsync()
    {
        await EnsureDatabaseAsync();

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await ExecuteAsync(connection, SchemaSql);
        await ExecuteAsync(connection, SeedSql);
        await EnsureDefaultAdminPasswordAsync(connection);
    }

    private async Task EnsureDatabaseAsync()
    {
        var databaseName = _connectionFactory.GetDatabaseName();
        await using var connection = _connectionFactory.CreateMasterConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            IF DB_ID(@DatabaseName) IS NULL
            BEGIN
                EXEC('CREATE DATABASE {QuoteSqlIdentifier(databaseName)}');
            END;
            """;
        command.Parameters.AddWithValue("@DatabaseName", databaseName);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task EnsureDefaultAdminPasswordAsync(SqlConnection connection)
    {
        var password = Environment.GetEnvironmentVariable("SMARTIMS_ADMIN_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            password = "admin";
        }

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.AppUser
            SET PasswordHash = @PasswordHash,
                UpdatedAt = SYSUTCDATETIME()
            WHERE UserName = N'Admin'
              AND (PasswordHash IS NULL OR LTRIM(RTRIM(PasswordHash)) = N'');
            """;
        command.Parameters.AddWithValue("@PasswordHash", PasswordHasher.HashPassword(password));
        await command.ExecuteNonQueryAsync();
    }

    private static string QuoteSqlIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException("Database name is required.");
        }

        return "[" + value.Replace("]", "]]", StringComparison.Ordinal) + "]";
    }

    private static async Task ExecuteAsync(SqlConnection connection, string commandText)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = commandText;
        await command.ExecuteNonQueryAsync();
    }

    private const string SchemaSql = """
        IF OBJECT_ID('dbo.AppUser', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.AppUser (
                AppUserID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                UserName NVARCHAR(100) NOT NULL,
                DisplayName NVARCHAR(100) NOT NULL,
                Department NVARCHAR(100) NULL,
                Email NVARCHAR(200) NULL,
                PasswordHash NVARCHAR(300) NULL,
                IsActive BIT NOT NULL CONSTRAINT DF_AppUser_IsActive DEFAULT (1),
                LastLoginAt DATETIME2(0) NULL,
                CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_AppUser_CreatedAt DEFAULT (SYSUTCDATETIME()),
                UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_AppUser_UpdatedAt DEFAULT (SYSUTCDATETIME())
            );
        END;

        IF COL_LENGTH('dbo.AppUser', 'Department') IS NULL
            ALTER TABLE dbo.AppUser ADD Department NVARCHAR(100) NULL;
        IF COL_LENGTH('dbo.AppUser', 'Email') IS NULL
            ALTER TABLE dbo.AppUser ADD Email NVARCHAR(200) NULL;
        IF COL_LENGTH('dbo.AppUser', 'PasswordHash') IS NULL
            ALTER TABLE dbo.AppUser ADD PasswordHash NVARCHAR(300) NULL;
        IF COL_LENGTH('dbo.AppUser', 'IsActive') IS NULL
            ALTER TABLE dbo.AppUser ADD IsActive BIT NOT NULL CONSTRAINT DF_AppUser_IsActive_Alter DEFAULT (1);
        IF COL_LENGTH('dbo.AppUser', 'LastLoginAt') IS NULL
            ALTER TABLE dbo.AppUser ADD LastLoginAt DATETIME2(0) NULL;
        IF COL_LENGTH('dbo.AppUser', 'UpdatedAt') IS NULL
            ALTER TABLE dbo.AppUser ADD UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_AppUser_UpdatedAt_Alter DEFAULT (SYSUTCDATETIME());
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_AppUser_UserName' AND object_id = OBJECT_ID('dbo.AppUser'))
            EXEC('CREATE UNIQUE INDEX UQ_AppUser_UserName ON dbo.AppUser(UserName);');

        IF OBJECT_ID('dbo.AppPermission', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.AppPermission (
                PermissionID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                PermissionCode NVARCHAR(100) NOT NULL,
                PermissionName NVARCHAR(100) NOT NULL,
                ModuleCode NVARCHAR(50) NOT NULL,
                ActionCode NVARCHAR(50) NOT NULL,
                Description NVARCHAR(500) NULL,
                IsActive BIT NOT NULL CONSTRAINT DF_AppPermission_IsActive DEFAULT (1),
                CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_AppPermission_CreatedAt DEFAULT (SYSUTCDATETIME())
            );
        END;
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_AppPermission_Code' AND object_id = OBJECT_ID('dbo.AppPermission'))
            EXEC('CREATE UNIQUE INDEX UQ_AppPermission_Code ON dbo.AppPermission(PermissionCode);');

        IF OBJECT_ID('dbo.UserPermission', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.UserPermission (
                AppUserID BIGINT NOT NULL,
                PermissionID BIGINT NOT NULL,
                CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UserPermission_CreatedAt DEFAULT (SYSUTCDATETIME()),
                CONSTRAINT PK_UserPermission PRIMARY KEY (AppUserID, PermissionID)
            );
        END;
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserPermission_User' AND parent_object_id = OBJECT_ID('dbo.UserPermission'))
            EXEC('ALTER TABLE dbo.UserPermission ADD CONSTRAINT FK_UserPermission_User FOREIGN KEY (AppUserID) REFERENCES dbo.AppUser(AppUserID) ON DELETE CASCADE;');
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserPermission_Permission' AND parent_object_id = OBJECT_ID('dbo.UserPermission'))
            EXEC('ALTER TABLE dbo.UserPermission ADD CONSTRAINT FK_UserPermission_Permission FOREIGN KEY (PermissionID) REFERENCES dbo.AppPermission(PermissionID) ON DELETE CASCADE;');

        IF OBJECT_ID('dbo.SystemSetting', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.SystemSetting (
                SystemSettingID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                SettingKey NVARCHAR(100) NOT NULL,
                SettingName NVARCHAR(100) NOT NULL,
                Category NVARCHAR(100) NOT NULL,
                SettingValue NVARCHAR(1000) NULL,
                Description NVARCHAR(500) NULL,
                IsActive BIT NOT NULL CONSTRAINT DF_SystemSetting_IsActive DEFAULT (1),
                UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_SystemSetting_UpdatedAt DEFAULT (SYSUTCDATETIME())
            );
        END;
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_SystemSetting_Key' AND object_id = OBJECT_ID('dbo.SystemSetting'))
            EXEC('CREATE UNIQUE INDEX UQ_SystemSetting_Key ON dbo.SystemSetting(SettingKey);');

        IF OBJECT_ID('dbo.UserListViewSetting', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.UserListViewSetting (
                AppUserID BIGINT NOT NULL,
                ListKey NVARCHAR(100) NOT NULL,
                SettingsJson NVARCHAR(MAX) NOT NULL,
                UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UserListViewSetting_UpdatedAt DEFAULT (SYSUTCDATETIME()),
                CONSTRAINT PK_UserListViewSetting PRIMARY KEY (AppUserID, ListKey)
            );
        END;
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_UserListViewSetting_User' AND parent_object_id = OBJECT_ID('dbo.UserListViewSetting'))
            EXEC('ALTER TABLE dbo.UserListViewSetting ADD CONSTRAINT FK_UserListViewSetting_User FOREIGN KEY (AppUserID) REFERENCES dbo.AppUser(AppUserID) ON DELETE CASCADE;');

        IF OBJECT_ID('dbo.AuditLog', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.AuditLog (
                AuditLogID BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                EventTime DATETIME2(0) NOT NULL CONSTRAINT DF_AuditLog_EventTime DEFAULT (SYSUTCDATETIME()),
                ActorUserName NVARCHAR(100) NULL,
                ModuleCode NVARCHAR(50) NOT NULL,
                ActionCode NVARCHAR(50) NOT NULL,
                EntityName NVARCHAR(100) NOT NULL,
                EntityID NVARCHAR(100) NULL,
                Summary NVARCHAR(500) NOT NULL,
                IpAddress NVARCHAR(100) NULL
            );
        END;
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AuditLog_EventTime' AND object_id = OBJECT_ID('dbo.AuditLog'))
            EXEC('CREATE INDEX IX_AuditLog_EventTime ON dbo.AuditLog(EventTime DESC);');
        """;

    private const string SeedSql = """
        MERGE dbo.AppPermission AS target
        USING (
            VALUES
              (N'PAGE_DASHBOARD', N'儀表板', N'PAGE', N'NAVIGATE', N'可進入系統首頁與基礎資料預留頁'),
              (N'PAGE_COMPONENT_TEST', N'元件測試頁面', N'PAGE', N'NAVIGATE', N'可進入元件測試頁面'),
              (N'PAGE_ADMIN_USERS', N'使用者管理', N'PAGE', N'NAVIGATE', N'可進入使用者管理'),
              (N'PAGE_ADMIN_PERMISSIONS', N'權限設定', N'PAGE', N'NAVIGATE', N'可進入使用者權限設定'),
              (N'PAGE_ADMIN_BRANDING', N'公司形象', N'PAGE', N'NAVIGATE', N'可進入公司 Logo 與登入圖設定'),
              (N'PAGE_DASHBOARD_FIND', N'儀表板-尋找', N'PAGE_DASHBOARD', N'FIND', N'可在儀表板使用尋找功能'),
              (N'PAGE_DASHBOARD_PRINT', N'儀表板-列印', N'PAGE_DASHBOARD', N'PRINT', N'可列印儀表板'),
              (N'PAGE_DASHBOARD_FIRST', N'儀表板-第一筆', N'PAGE_DASHBOARD', N'FIRST', N'可在儀表板移至第一筆'),
              (N'PAGE_DASHBOARD_PREVIOUS', N'儀表板-上一筆', N'PAGE_DASHBOARD', N'PREVIOUS', N'可在儀表板移至上一筆'),
              (N'PAGE_DASHBOARD_NEXT', N'儀表板-下一筆', N'PAGE_DASHBOARD', N'NEXT', N'可在儀表板移至下一筆'),
              (N'PAGE_DASHBOARD_LAST', N'儀表板-最後一筆', N'PAGE_DASHBOARD', N'LAST', N'可在儀表板移至最後一筆'),
              (N'PAGE_COMPONENT_TEST_FIND', N'元件測試-尋找', N'PAGE_COMPONENT_TEST', N'FIND', N'可在元件測試頁面使用尋找功能'),
              (N'PAGE_COMPONENT_TEST_PRINT', N'元件測試-列印', N'PAGE_COMPONENT_TEST', N'PRINT', N'可列印元件測試頁面'),
              (N'PAGE_COMPONENT_TEST_FIRST', N'元件測試-第一筆', N'PAGE_COMPONENT_TEST', N'FIRST', N'可在元件測試頁面移至第一筆'),
              (N'PAGE_COMPONENT_TEST_PREVIOUS', N'元件測試-上一筆', N'PAGE_COMPONENT_TEST', N'PREVIOUS', N'可在元件測試頁面移至上一筆'),
              (N'PAGE_COMPONENT_TEST_NEXT', N'元件測試-下一筆', N'PAGE_COMPONENT_TEST', N'NEXT', N'可在元件測試頁面移至下一筆'),
              (N'PAGE_COMPONENT_TEST_LAST', N'元件測試-最後一筆', N'PAGE_COMPONENT_TEST', N'LAST', N'可在元件測試頁面移至最後一筆'),
              (N'PAGE_ADMIN_USERS_CREATE', N'使用者管理-新增', N'PAGE_ADMIN_USERS', N'CREATE', N'可在使用者管理新增資料'),
              (N'PAGE_ADMIN_USERS_DELETE', N'使用者管理-刪除', N'PAGE_ADMIN_USERS', N'DELETE', N'可在使用者管理刪除資料'),
              (N'PAGE_ADMIN_USERS_EDIT', N'使用者管理-修改', N'PAGE_ADMIN_USERS', N'EDIT', N'可在使用者管理修改資料'),
              (N'PAGE_ADMIN_USERS_FIND', N'使用者管理-尋找', N'PAGE_ADMIN_USERS', N'FIND', N'可在使用者管理使用尋找功能'),
              (N'PAGE_ADMIN_USERS_PRINT', N'使用者管理-列印', N'PAGE_ADMIN_USERS', N'PRINT', N'可列印使用者管理'),
              (N'PAGE_ADMIN_USERS_FIRST', N'使用者管理-第一筆', N'PAGE_ADMIN_USERS', N'FIRST', N'可在使用者管理移至第一筆'),
              (N'PAGE_ADMIN_USERS_PREVIOUS', N'使用者管理-上一筆', N'PAGE_ADMIN_USERS', N'PREVIOUS', N'可在使用者管理移至上一筆'),
              (N'PAGE_ADMIN_USERS_NEXT', N'使用者管理-下一筆', N'PAGE_ADMIN_USERS', N'NEXT', N'可在使用者管理移至下一筆'),
              (N'PAGE_ADMIN_USERS_LAST', N'使用者管理-最後一筆', N'PAGE_ADMIN_USERS', N'LAST', N'可在使用者管理移至最後一筆'),
              (N'PAGE_ADMIN_PERMISSIONS_EDIT', N'權限設定-修改', N'PAGE_ADMIN_PERMISSIONS', N'EDIT', N'可在權限設定修改資料'),
              (N'PAGE_ADMIN_PERMISSIONS_FIND', N'權限設定-尋找', N'PAGE_ADMIN_PERMISSIONS', N'FIND', N'可在權限設定使用尋找功能'),
              (N'PAGE_ADMIN_PERMISSIONS_PRINT', N'權限設定-列印', N'PAGE_ADMIN_PERMISSIONS', N'PRINT', N'可列印權限設定'),
              (N'PAGE_ADMIN_PERMISSIONS_FIRST', N'權限設定-第一筆', N'PAGE_ADMIN_PERMISSIONS', N'FIRST', N'可在權限設定移至第一筆'),
              (N'PAGE_ADMIN_PERMISSIONS_PREVIOUS', N'權限設定-上一筆', N'PAGE_ADMIN_PERMISSIONS', N'PREVIOUS', N'可在權限設定移至上一筆'),
              (N'PAGE_ADMIN_PERMISSIONS_NEXT', N'權限設定-下一筆', N'PAGE_ADMIN_PERMISSIONS', N'NEXT', N'可在權限設定移至下一筆'),
              (N'PAGE_ADMIN_PERMISSIONS_LAST', N'權限設定-最後一筆', N'PAGE_ADMIN_PERMISSIONS', N'LAST', N'可在權限設定移至最後一筆'),
              (N'PAGE_ADMIN_BRANDING_EDIT', N'公司形象-修改', N'PAGE_ADMIN_BRANDING', N'EDIT', N'可在公司形象修改資料'),
              (N'PAGE_ADMIN_BRANDING_FIND', N'公司形象-尋找', N'PAGE_ADMIN_BRANDING', N'FIND', N'可在公司形象使用尋找功能'),
              (N'PAGE_ADMIN_BRANDING_PRINT', N'公司形象-列印', N'PAGE_ADMIN_BRANDING', N'PRINT', N'可列印公司形象設定'),
              (N'ADMIN_USERS_MANAGE', N'維護使用者', N'ADMIN', N'MANAGE', N'可新增、編輯、停用與重設使用者密碼'),
              (N'ADMIN_PERMISSIONS_MANAGE', N'維護使用者權限', N'ADMIN', N'MANAGE', N'可調整使用者直綁權限'),
              (N'ADMIN_BRANDING_MANAGE', N'維護公司形象', N'ADMIN', N'MANAGE', N'可調整公司名稱、Logo 與登入形象圖')
        ) AS source (PermissionCode, PermissionName, ModuleCode, ActionCode, Description)
        ON target.PermissionCode = source.PermissionCode
        WHEN MATCHED THEN
          UPDATE SET PermissionName = source.PermissionName, ModuleCode = source.ModuleCode, ActionCode = source.ActionCode, Description = source.Description, IsActive = 1
        WHEN NOT MATCHED THEN
          INSERT (PermissionCode, PermissionName, ModuleCode, ActionCode, Description, IsActive)
          VALUES (source.PermissionCode, source.PermissionName, source.ModuleCode, source.ActionCode, source.Description, 1);

        MERGE dbo.AppUser AS target
        USING (VALUES (N'Admin', N'系統管理員', N'資訊部', N'admin@example.com', CAST(1 AS BIT))) AS source (UserName, DisplayName, Department, Email, IsActive)
        ON target.UserName = source.UserName
        WHEN NOT MATCHED THEN
          INSERT (UserName, DisplayName, Department, Email, IsActive)
          VALUES (source.UserName, source.DisplayName, source.Department, source.Email, source.IsActive);

        INSERT INTO dbo.UserPermission (AppUserID, PermissionID)
        SELECT u.AppUserID, p.PermissionID
        FROM dbo.AppUser u
        CROSS JOIN dbo.AppPermission p
        WHERE u.UserName = N'Admin'
          AND NOT EXISTS (
            SELECT 1 FROM dbo.UserPermission existing
            WHERE existing.AppUserID = u.AppUserID AND existing.PermissionID = p.PermissionID
          );

        MERGE dbo.SystemSetting AS target
        USING (
            VALUES
              (N'COMPANY_NAME', N'公司名稱', N'公司形象', N'智慧進銷存系統', N'顯示於 Header 與系統標題', CAST(1 AS BIT)),
              (N'COMPANY_LOGO_PATH', N'公司 Logo', N'公司形象', N'', N'Header 與 Login 使用的 Logo 路徑', CAST(1 AS BIT)),
              (N'LOGIN_HERO_IMAGE_PATH', N'登入形象圖', N'公司形象', N'', N'登入頁左側形象圖路徑', CAST(1 AS BIT)),
              (N'LOGIN_TITLE', N'登入標題', N'公司形象', N'智慧進銷存系統', N'登入頁主標題', CAST(1 AS BIT)),
              (N'LOGIN_SUBTITLE', N'登入副標題', N'公司形象', N'請使用管理員提供的帳號登入', N'登入頁副標題', CAST(1 AS BIT))
        ) AS source (SettingKey, SettingName, Category, SettingValue, Description, IsActive)
        ON target.SettingKey = source.SettingKey
        WHEN NOT MATCHED THEN
          INSERT (SettingKey, SettingName, Category, SettingValue, Description, IsActive)
          VALUES (source.SettingKey, source.SettingName, source.Category, source.SettingValue, source.Description, source.IsActive);
        """;
}
