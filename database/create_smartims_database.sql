:setvar DatabaseName "SmartIMS_NewVersion"

IF DB_ID(N'$(DatabaseName)') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [$(DatabaseName)]');
END;
GO

USE [$(DatabaseName)];
GO

IF OBJECT_ID(N'dbo.AppUser', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppUser (
        AppUserID BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppUser PRIMARY KEY,
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
GO

IF COL_LENGTH(N'dbo.AppUser', N'Department') IS NULL
    ALTER TABLE dbo.AppUser ADD Department NVARCHAR(100) NULL;
IF COL_LENGTH(N'dbo.AppUser', N'Email') IS NULL
    ALTER TABLE dbo.AppUser ADD Email NVARCHAR(200) NULL;
IF COL_LENGTH(N'dbo.AppUser', N'PasswordHash') IS NULL
    ALTER TABLE dbo.AppUser ADD PasswordHash NVARCHAR(300) NULL;
IF COL_LENGTH(N'dbo.AppUser', N'IsActive') IS NULL
    ALTER TABLE dbo.AppUser ADD IsActive BIT NOT NULL CONSTRAINT DF_AppUser_IsActive_Alter DEFAULT (1);
IF COL_LENGTH(N'dbo.AppUser', N'LastLoginAt') IS NULL
    ALTER TABLE dbo.AppUser ADD LastLoginAt DATETIME2(0) NULL;
IF COL_LENGTH(N'dbo.AppUser', N'UpdatedAt') IS NULL
    ALTER TABLE dbo.AppUser ADD UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_AppUser_UpdatedAt_Alter DEFAULT (SYSUTCDATETIME());
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_AppUser_UserName' AND object_id = OBJECT_ID(N'dbo.AppUser'))
    CREATE UNIQUE INDEX UQ_AppUser_UserName ON dbo.AppUser(UserName);
GO

IF OBJECT_ID(N'dbo.AppPermission', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppPermission (
        PermissionID BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppPermission PRIMARY KEY,
        PermissionCode NVARCHAR(100) NOT NULL,
        PermissionName NVARCHAR(100) NOT NULL,
        ModuleCode NVARCHAR(50) NOT NULL,
        ActionCode NVARCHAR(50) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_AppPermission_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_AppPermission_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_AppPermission_Code' AND object_id = OBJECT_ID(N'dbo.AppPermission'))
    CREATE UNIQUE INDEX UQ_AppPermission_Code ON dbo.AppPermission(PermissionCode);
GO

IF OBJECT_ID(N'dbo.UserPermission', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserPermission (
        AppUserID BIGINT NOT NULL,
        PermissionID BIGINT NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UserPermission_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_UserPermission PRIMARY KEY (AppUserID, PermissionID)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UserPermission_User' AND parent_object_id = OBJECT_ID(N'dbo.UserPermission'))
    ALTER TABLE dbo.UserPermission ADD CONSTRAINT FK_UserPermission_User FOREIGN KEY (AppUserID) REFERENCES dbo.AppUser(AppUserID) ON DELETE CASCADE;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UserPermission_Permission' AND parent_object_id = OBJECT_ID(N'dbo.UserPermission'))
    ALTER TABLE dbo.UserPermission ADD CONSTRAINT FK_UserPermission_Permission FOREIGN KEY (PermissionID) REFERENCES dbo.AppPermission(PermissionID) ON DELETE CASCADE;
GO

IF OBJECT_ID(N'dbo.SystemSetting', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemSetting (
        SystemSettingID BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SystemSetting PRIMARY KEY,
        SettingKey NVARCHAR(100) NOT NULL,
        SettingName NVARCHAR(100) NOT NULL,
        Category NVARCHAR(100) NOT NULL,
        SettingValue NVARCHAR(1000) NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_SystemSetting_IsActive DEFAULT (1),
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_SystemSetting_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_SystemSetting_Key' AND object_id = OBJECT_ID(N'dbo.SystemSetting'))
    CREATE UNIQUE INDEX UQ_SystemSetting_Key ON dbo.SystemSetting(SettingKey);
GO

IF OBJECT_ID(N'dbo.UserListViewSetting', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserListViewSetting (
        AppUserID BIGINT NOT NULL,
        ListKey NVARCHAR(100) NOT NULL,
        SettingsJson NVARCHAR(MAX) NOT NULL,
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UserListViewSetting_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_UserListViewSetting PRIMARY KEY (AppUserID, ListKey)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UserListViewSetting_User' AND parent_object_id = OBJECT_ID(N'dbo.UserListViewSetting'))
    ALTER TABLE dbo.UserListViewSetting ADD CONSTRAINT FK_UserListViewSetting_User FOREIGN KEY (AppUserID) REFERENCES dbo.AppUser(AppUserID) ON DELETE CASCADE;
GO

IF OBJECT_ID(N'dbo.AuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLog (
        AuditLogID BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLog PRIMARY KEY,
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
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLog_EventTime' AND object_id = OBJECT_ID(N'dbo.AuditLog'))
    CREATE INDEX IX_AuditLog_EventTime ON dbo.AuditLog(EventTime DESC);
GO

MERGE dbo.AppPermission AS target
USING (
    VALUES
        (N'PAGE_DASHBOARD', N'儀表板', N'PAGE', N'NAVIGATE', N'允許進入系統首頁與儀表板。'),
        (N'PAGE_COMPONENT_TEST', N'元件測試頁', N'PAGE', N'NAVIGATE', N'允許進入元件測試頁。'),
        (N'PAGE_CUSTOMER_LAYOUT_TEST', N'客戶資料管理測試', N'PAGE', N'NAVIGATE', N'允許進入客戶資料管理 Layout 測試頁。'),
        (N'PAGE_ADMIN_USERS', N'使用者管理', N'PAGE', N'NAVIGATE', N'允許進入使用者管理頁。'),
        (N'PAGE_ADMIN_PERMISSIONS', N'權限設定', N'PAGE', N'NAVIGATE', N'允許進入使用者權限設定頁。'),
        (N'PAGE_ADMIN_BRANDING', N'品牌設定', N'PAGE', N'NAVIGATE', N'允許進入公司名稱、Logo 與登入頁設定。'),
        (N'PAGE_DASHBOARD_FIND', N'儀表板-尋找', N'PAGE_DASHBOARD', N'FIND', N'允許在儀表板使用尋找功能。'),
        (N'PAGE_DASHBOARD_PRINT', N'儀表板-列印', N'PAGE_DASHBOARD', N'PRINT', N'允許列印儀表板。'),
        (N'PAGE_DASHBOARD_FIRST', N'儀表板-第一筆', N'PAGE_DASHBOARD', N'FIRST', N'允許在儀表板移至第一筆。'),
        (N'PAGE_DASHBOARD_PREVIOUS', N'儀表板-上一筆', N'PAGE_DASHBOARD', N'PREVIOUS', N'允許在儀表板移至上一筆。'),
        (N'PAGE_DASHBOARD_NEXT', N'儀表板-下一筆', N'PAGE_DASHBOARD', N'NEXT', N'允許在儀表板移至下一筆。'),
        (N'PAGE_DASHBOARD_LAST', N'儀表板-最後一筆', N'PAGE_DASHBOARD', N'LAST', N'允許在儀表板移至最後一筆。'),
        (N'PAGE_COMPONENT_TEST_FIND', N'元件測試-尋找', N'PAGE_COMPONENT_TEST', N'FIND', N'允許在元件測試頁使用尋找功能。'),
        (N'PAGE_COMPONENT_TEST_PRINT', N'元件測試-列印', N'PAGE_COMPONENT_TEST', N'PRINT', N'允許列印元件測試頁。'),
        (N'PAGE_COMPONENT_TEST_CREATE', N'元件測試-新增', N'PAGE_COMPONENT_TEST', N'CREATE', N'允許在元件測試頁新增測試資料。'),
        (N'PAGE_COMPONENT_TEST_DELETE', N'元件測試-刪除', N'PAGE_COMPONENT_TEST', N'DELETE', N'允許在元件測試頁刪除測試資料。'),
        (N'PAGE_COMPONENT_TEST_COPY', N'元件測試-複製', N'PAGE_COMPONENT_TEST', N'COPY', N'允許在元件測試頁複製測試資料。'),
        (N'PAGE_COMPONENT_TEST_EDIT', N'元件測試-修改', N'PAGE_COMPONENT_TEST', N'EDIT', N'允許在元件測試頁修改測試資料。'),
        (N'PAGE_COMPONENT_TEST_CANCEL', N'元件測試-取消', N'PAGE_COMPONENT_TEST', N'CANCEL', N'允許在元件測試頁取消目前編輯。'),
        (N'PAGE_COMPONENT_TEST_FIRST', N'元件測試-第一筆', N'PAGE_COMPONENT_TEST', N'FIRST', N'允許在元件測試頁移至第一筆。'),
        (N'PAGE_COMPONENT_TEST_PREVIOUS', N'元件測試-上一筆', N'PAGE_COMPONENT_TEST', N'PREVIOUS', N'允許在元件測試頁移至上一筆。'),
        (N'PAGE_COMPONENT_TEST_NEXT', N'元件測試-下一筆', N'PAGE_COMPONENT_TEST', N'NEXT', N'允許在元件測試頁移至下一筆。'),
        (N'PAGE_COMPONENT_TEST_LAST', N'元件測試-最後一筆', N'PAGE_COMPONENT_TEST', N'LAST', N'允許在元件測試頁移至最後一筆。'),
        (N'PAGE_CUSTOMER_LAYOUT_TEST_CREATE', N'客戶資料管理-新增', N'PAGE_CUSTOMER_LAYOUT_TEST', N'CREATE', N'允許在客戶資料管理測試頁新增資料。'),
        (N'PAGE_CUSTOMER_LAYOUT_TEST_DELETE', N'客戶資料管理-刪除', N'PAGE_CUSTOMER_LAYOUT_TEST', N'DELETE', N'允許在客戶資料管理測試頁刪除資料。'),
        (N'PAGE_CUSTOMER_LAYOUT_TEST_EDIT', N'客戶資料管理-修改', N'PAGE_CUSTOMER_LAYOUT_TEST', N'EDIT', N'允許在客戶資料管理測試頁修改資料。'),
        (N'PAGE_CUSTOMER_LAYOUT_TEST_FIND', N'客戶資料管理-尋找', N'PAGE_CUSTOMER_LAYOUT_TEST', N'FIND', N'允許在客戶資料管理測試頁尋找資料。'),
        (N'PAGE_CUSTOMER_LAYOUT_TEST_PRINT', N'客戶資料管理-列印', N'PAGE_CUSTOMER_LAYOUT_TEST', N'PRINT', N'允許列印客戶資料管理測試頁。'),
        (N'PAGE_CUSTOMER_LAYOUT_TEST_FIRST', N'客戶資料管理-第一筆', N'PAGE_CUSTOMER_LAYOUT_TEST', N'FIRST', N'允許在客戶資料管理測試頁移至第一筆。'),
        (N'PAGE_CUSTOMER_LAYOUT_TEST_PREVIOUS', N'客戶資料管理-上一筆', N'PAGE_CUSTOMER_LAYOUT_TEST', N'PREVIOUS', N'允許在客戶資料管理測試頁移至上一筆。'),
        (N'PAGE_CUSTOMER_LAYOUT_TEST_NEXT', N'客戶資料管理-下一筆', N'PAGE_CUSTOMER_LAYOUT_TEST', N'NEXT', N'允許在客戶資料管理測試頁移至下一筆。'),
        (N'PAGE_CUSTOMER_LAYOUT_TEST_LAST', N'客戶資料管理-最後一筆', N'PAGE_CUSTOMER_LAYOUT_TEST', N'LAST', N'允許在客戶資料管理測試頁移至最後一筆。'),
        (N'PAGE_ADMIN_USERS_CREATE', N'使用者管理-新增', N'PAGE_ADMIN_USERS', N'CREATE', N'允許在使用者管理新增資料。'),
        (N'PAGE_ADMIN_USERS_DELETE', N'使用者管理-刪除', N'PAGE_ADMIN_USERS', N'DELETE', N'允許在使用者管理刪除資料。'),
        (N'PAGE_ADMIN_USERS_EDIT', N'使用者管理-修改', N'PAGE_ADMIN_USERS', N'EDIT', N'允許在使用者管理修改資料。'),
        (N'PAGE_ADMIN_USERS_FIND', N'使用者管理-尋找', N'PAGE_ADMIN_USERS', N'FIND', N'允許在使用者管理使用尋找功能。'),
        (N'PAGE_ADMIN_USERS_PRINT', N'使用者管理-列印', N'PAGE_ADMIN_USERS', N'PRINT', N'允許列印使用者管理。'),
        (N'PAGE_ADMIN_USERS_FIRST', N'使用者管理-第一筆', N'PAGE_ADMIN_USERS', N'FIRST', N'允許在使用者管理移至第一筆。'),
        (N'PAGE_ADMIN_USERS_PREVIOUS', N'使用者管理-上一筆', N'PAGE_ADMIN_USERS', N'PREVIOUS', N'允許在使用者管理移至上一筆。'),
        (N'PAGE_ADMIN_USERS_NEXT', N'使用者管理-下一筆', N'PAGE_ADMIN_USERS', N'NEXT', N'允許在使用者管理移至下一筆。'),
        (N'PAGE_ADMIN_USERS_LAST', N'使用者管理-最後一筆', N'PAGE_ADMIN_USERS', N'LAST', N'允許在使用者管理移至最後一筆。'),
        (N'PAGE_ADMIN_PERMISSIONS_EDIT', N'權限設定-修改', N'PAGE_ADMIN_PERMISSIONS', N'EDIT', N'允許在權限設定修改資料。'),
        (N'PAGE_ADMIN_PERMISSIONS_FIND', N'權限設定-尋找', N'PAGE_ADMIN_PERMISSIONS', N'FIND', N'允許在權限設定使用尋找功能。'),
        (N'PAGE_ADMIN_PERMISSIONS_PRINT', N'權限設定-列印', N'PAGE_ADMIN_PERMISSIONS', N'PRINT', N'允許列印權限設定。'),
        (N'PAGE_ADMIN_PERMISSIONS_FIRST', N'權限設定-第一筆', N'PAGE_ADMIN_PERMISSIONS', N'FIRST', N'允許在權限設定移至第一筆。'),
        (N'PAGE_ADMIN_PERMISSIONS_PREVIOUS', N'權限設定-上一筆', N'PAGE_ADMIN_PERMISSIONS', N'PREVIOUS', N'允許在權限設定移至上一筆。'),
        (N'PAGE_ADMIN_PERMISSIONS_NEXT', N'權限設定-下一筆', N'PAGE_ADMIN_PERMISSIONS', N'NEXT', N'允許在權限設定移至下一筆。'),
        (N'PAGE_ADMIN_PERMISSIONS_LAST', N'權限設定-最後一筆', N'PAGE_ADMIN_PERMISSIONS', N'LAST', N'允許在權限設定移至最後一筆。'),
        (N'PAGE_ADMIN_BRANDING_EDIT', N'品牌設定-修改', N'PAGE_ADMIN_BRANDING', N'EDIT', N'允許在品牌設定修改資料。'),
        (N'PAGE_ADMIN_BRANDING_FIND', N'品牌設定-尋找', N'PAGE_ADMIN_BRANDING', N'FIND', N'允許在品牌設定使用尋找功能。'),
        (N'PAGE_ADMIN_BRANDING_PRINT', N'品牌設定-列印', N'PAGE_ADMIN_BRANDING', N'PRINT', N'允許列印品牌設定。'),
        (N'ADMIN_USERS_MANAGE', N'管理使用者', N'ADMIN', N'MANAGE', N'允許新增、編輯、啟用或停用使用者。'),
        (N'ADMIN_PERMISSIONS_MANAGE', N'管理使用者權限', N'ADMIN', N'MANAGE', N'允許調整使用者的系統權限。'),
        (N'ADMIN_BRANDING_MANAGE', N'管理品牌設定', N'ADMIN', N'MANAGE', N'允許修改公司名稱、Logo 與登入頁內容。')
) AS source (PermissionCode, PermissionName, ModuleCode, ActionCode, Description)
ON target.PermissionCode = source.PermissionCode
WHEN MATCHED THEN
    UPDATE SET
        PermissionName = source.PermissionName,
        ModuleCode = source.ModuleCode,
        ActionCode = source.ActionCode,
        Description = source.Description,
        IsActive = 1
WHEN NOT MATCHED THEN
    INSERT (PermissionCode, PermissionName, ModuleCode, ActionCode, Description, IsActive)
    VALUES (source.PermissionCode, source.PermissionName, source.ModuleCode, source.ActionCode, source.Description, 1);
GO

MERGE dbo.AppUser AS target
USING (VALUES (N'Admin', N'系統管理員', N'資訊部', N'admin@example.com', CAST(1 AS BIT))) AS source (UserName, DisplayName, Department, Email, IsActive)
ON target.UserName = source.UserName
WHEN MATCHED THEN
    UPDATE SET
        DisplayName = source.DisplayName,
        Department = source.Department,
        Email = source.Email,
        IsActive = source.IsActive,
        UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (UserName, DisplayName, Department, Email, IsActive)
    VALUES (source.UserName, source.DisplayName, source.Department, source.Email, source.IsActive);
GO

INSERT INTO dbo.UserPermission (AppUserID, PermissionID)
SELECT u.AppUserID, p.PermissionID
FROM dbo.AppUser u
CROSS JOIN dbo.AppPermission p
WHERE u.UserName = N'Admin'
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.UserPermission existing
      WHERE existing.AppUserID = u.AppUserID
        AND existing.PermissionID = p.PermissionID
  );
GO

MERGE dbo.SystemSetting AS target
USING (
    VALUES
        (N'COMPANY_NAME', N'公司名稱', N'品牌設定', N'新版進銷存系統', N'顯示於 Header 與登入頁的公司名稱。', CAST(1 AS BIT)),
        (N'COMPANY_LOGO_PATH', N'公司 Logo', N'品牌設定', N'', N'Header 與登入頁使用的 Logo 路徑。', CAST(1 AS BIT)),
        (N'LOGIN_HERO_IMAGE_PATH', N'登入背景圖', N'品牌設定', N'', N'登入頁左側或背景圖片路徑。', CAST(1 AS BIT)),
        (N'LOGIN_TITLE', N'登入標題', N'品牌設定', N'新版進銷存系統', N'登入頁主要標題。', CAST(1 AS BIT)),
        (N'LOGIN_SUBTITLE', N'登入說明', N'品牌設定', N'請輸入帳號與密碼登入系統。', N'登入頁副標題說明。', CAST(1 AS BIT))
) AS source (SettingKey, SettingName, Category, SettingValue, Description, IsActive)
ON target.SettingKey = source.SettingKey
WHEN MATCHED THEN
    UPDATE SET
        SettingName = source.SettingName,
        Category = source.Category,
        SettingValue = source.SettingValue,
        Description = source.Description,
        IsActive = source.IsActive,
        UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (SettingKey, SettingName, Category, SettingValue, Description, IsActive)
    VALUES (source.SettingKey, source.SettingName, source.Category, source.SettingValue, source.Description, source.IsActive);
GO

PRINT N'SmartIMS database setup completed.';
GO
