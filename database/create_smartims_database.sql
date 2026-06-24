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

IF OBJECT_ID(N'dbo.AppPage', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppPage (
        PageID BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppPage PRIMARY KEY,
        PageCode NVARCHAR(100) NOT NULL,
        PageName NVARCHAR(100) NOT NULL,
        MenuGroup NVARCHAR(100) NOT NULL,
        ControllerName NVARCHAR(100) NOT NULL,
        ActionName NVARCHAR(100) NOT NULL,
        MenuSortOrder INT NOT NULL CONSTRAINT DF_AppPage_MenuSortOrder DEFAULT (0),
        SortOrder INT NOT NULL CONSTRAINT DF_AppPage_SortOrder DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_AppPage_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_AppPage_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_AppPage_Code' AND object_id = OBJECT_ID(N'dbo.AppPage'))
    CREATE UNIQUE INDEX UQ_AppPage_Code ON dbo.AppPage(PageCode);
GO

IF OBJECT_ID(N'dbo.AppAction', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppAction (
        ActionID BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppAction PRIMARY KEY,
        ActionCode NVARCHAR(50) NOT NULL,
        ActionName NVARCHAR(100) NOT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_AppAction_SortOrder DEFAULT (0),
        IsSystemAction BIT NOT NULL CONSTRAINT DF_AppAction_IsSystemAction DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_AppAction_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_AppAction_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_AppAction_Code' AND object_id = OBJECT_ID(N'dbo.AppAction'))
    CREATE UNIQUE INDEX UQ_AppAction_Code ON dbo.AppAction(ActionCode);
GO

IF OBJECT_ID(N'dbo.PageAction', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PageAction (
        PageActionID BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PageAction PRIMARY KEY,
        PageID BIGINT NOT NULL,
        ActionID BIGINT NOT NULL,
        PermissionCode NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_PageAction_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_PageAction_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_PageAction_Page_Action' AND object_id = OBJECT_ID(N'dbo.PageAction'))
    CREATE UNIQUE INDEX UQ_PageAction_Page_Action ON dbo.PageAction(PageID, ActionID);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_PageAction_PermissionCode' AND object_id = OBJECT_ID(N'dbo.PageAction'))
    CREATE UNIQUE INDEX UQ_PageAction_PermissionCode ON dbo.PageAction(PermissionCode);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PageAction_Page' AND parent_object_id = OBJECT_ID(N'dbo.PageAction'))
    ALTER TABLE dbo.PageAction ADD CONSTRAINT FK_PageAction_Page FOREIGN KEY (PageID) REFERENCES dbo.AppPage(PageID) ON DELETE CASCADE;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PageAction_Action' AND parent_object_id = OBJECT_ID(N'dbo.PageAction'))
    ALTER TABLE dbo.PageAction ADD CONSTRAINT FK_PageAction_Action FOREIGN KEY (ActionID) REFERENCES dbo.AppAction(ActionID) ON DELETE CASCADE;
GO

IF OBJECT_ID(N'dbo.AppRole', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppRole (
        RoleID BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AppRole PRIMARY KEY,
        RoleCode NVARCHAR(100) NOT NULL,
        RoleName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        IsSystemRole BIT NOT NULL CONSTRAINT DF_AppRole_IsSystemRole DEFAULT (0),
        IsActive BIT NOT NULL CONSTRAINT DF_AppRole_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_AppRole_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_AppRole_Code' AND object_id = OBJECT_ID(N'dbo.AppRole'))
    CREATE UNIQUE INDEX UQ_AppRole_Code ON dbo.AppRole(RoleCode);
GO

IF OBJECT_ID(N'dbo.UserRole', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRole (
        AppUserID BIGINT NOT NULL,
        RoleID BIGINT NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_UserRole_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_UserRole PRIMARY KEY (AppUserID, RoleID)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UserRole_User' AND parent_object_id = OBJECT_ID(N'dbo.UserRole'))
    ALTER TABLE dbo.UserRole ADD CONSTRAINT FK_UserRole_User FOREIGN KEY (AppUserID) REFERENCES dbo.AppUser(AppUserID) ON DELETE CASCADE;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UserRole_Role' AND parent_object_id = OBJECT_ID(N'dbo.UserRole'))
    ALTER TABLE dbo.UserRole ADD CONSTRAINT FK_UserRole_Role FOREIGN KEY (RoleID) REFERENCES dbo.AppRole(RoleID) ON DELETE CASCADE;
GO

IF OBJECT_ID(N'dbo.RolePageAction', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePageAction (
        RoleID BIGINT NOT NULL,
        PageActionID BIGINT NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_RolePageAction_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_RolePageAction PRIMARY KEY (RoleID, PageActionID)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RolePageAction_Role' AND parent_object_id = OBJECT_ID(N'dbo.RolePageAction'))
    ALTER TABLE dbo.RolePageAction ADD CONSTRAINT FK_RolePageAction_Role FOREIGN KEY (RoleID) REFERENCES dbo.AppRole(RoleID) ON DELETE CASCADE;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_RolePageAction_PageAction' AND parent_object_id = OBJECT_ID(N'dbo.RolePageAction'))
    ALTER TABLE dbo.RolePageAction ADD CONSTRAINT FK_RolePageAction_PageAction FOREIGN KEY (PageActionID) REFERENCES dbo.PageAction(PageActionID) ON DELETE CASCADE;
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

IF OBJECT_ID(N'dbo.ProductMaterial', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductMaterial (
        ProductMaterialID BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductMaterial PRIMARY KEY,
        MaterialCode NVARCHAR(50) NOT NULL,
        MaterialName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ProductMaterial_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ProductMaterial_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ProductMaterial_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_ProductMaterial_Code' AND object_id = OBJECT_ID(N'dbo.ProductMaterial'))
    CREATE UNIQUE INDEX UQ_ProductMaterial_Code ON dbo.ProductMaterial(MaterialCode) WHERE IsActive = 1;
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

MERGE dbo.AppAction AS target
USING (
    VALUES
        (N'NAVIGATE', N'進入', 10, CAST(1 AS BIT)),
        (N'CREATE', N'新增', 20, CAST(0 AS BIT)),
        (N'DELETE', N'刪除', 30, CAST(0 AS BIT)),
        (N'COPY', N'複製', 40, CAST(0 AS BIT)),
        (N'EDIT', N'修改', 50, CAST(0 AS BIT)),
        (N'CANCEL', N'取消', 60, CAST(0 AS BIT)),
        (N'FIND', N'尋找', 70, CAST(0 AS BIT)),
        (N'PRINT', N'列印', 80, CAST(0 AS BIT)),
        (N'FIRST', N'第一筆', 90, CAST(0 AS BIT)),
        (N'PREVIOUS', N'上一筆', 100, CAST(0 AS BIT)),
        (N'NEXT', N'下一筆', 110, CAST(0 AS BIT)),
        (N'LAST', N'最後一筆', 120, CAST(0 AS BIT))
) AS source (ActionCode, ActionName, SortOrder, IsSystemAction)
ON target.ActionCode = source.ActionCode
WHEN MATCHED THEN
    UPDATE SET ActionName = source.ActionName, SortOrder = source.SortOrder, IsSystemAction = source.IsSystemAction, IsActive = 1
WHEN NOT MATCHED THEN
    INSERT (ActionCode, ActionName, SortOrder, IsSystemAction, IsActive)
    VALUES (source.ActionCode, source.ActionName, source.SortOrder, source.IsSystemAction, 1);
GO

MERGE dbo.AppPage AS target
USING (
    VALUES
        (N'PAGE_ADMIN_USERS', N'使用者管理', N'系統管理', N'Admin', N'Users', 10, 10),
        (N'PAGE_ADMIN_PERMISSIONS', N'權限設定', N'系統管理', N'Admin', N'Permissions', 10, 20),
        (N'PAGE_ADMIN_BRANDING', N'公司形象', N'系統管理', N'Admin', N'Branding', 10, 30),
        (N'PAGE_DASHBOARD', N'基礎資料預留', N'基礎資料', N'Home', N'Index', 20, 10),
        (N'PAGE_COMPONENT_TEST', N'元件測試頁面', N'基礎資料', N'Home', N'ComponentTest', 20, 20),
        (N'PAGE_CUSTOMER_LAYOUT_TEST', N'客戶資料管理', N'基礎資料', N'Home', N'CustomerLayoutTest', 20, 30),
        (N'PAGE_PRODUCT_LAYOUT_TEST', N'商品資料管理', N'基礎資料', N'Home', N'ProductLayoutTest', 20, 40),
        (N'PAGE_PRODUCT_MATERIAL', N'商品材質管理', N'基礎資料', N'Home', N'MaterialLayoutTest', 20, 50),
        (N'PAGE_SUPPLIER', N'供應商管理', N'基礎資料', N'Home', N'SupplierLayoutTest', 20, 60)
) AS source (PageCode, PageName, MenuGroup, ControllerName, ActionName, MenuSortOrder, SortOrder)
ON target.PageCode = source.PageCode
WHEN MATCHED THEN
    UPDATE SET PageName = source.PageName, MenuGroup = source.MenuGroup, ControllerName = source.ControllerName, ActionName = source.ActionName,
               MenuSortOrder = source.MenuSortOrder, SortOrder = source.SortOrder, IsActive = 1
WHEN NOT MATCHED THEN
    INSERT (PageCode, PageName, MenuGroup, ControllerName, ActionName, MenuSortOrder, SortOrder, IsActive)
    VALUES (source.PageCode, source.PageName, source.MenuGroup, source.ControllerName, source.ActionName, source.MenuSortOrder, source.SortOrder, 1);
GO

MERGE dbo.PageAction AS target
USING (
    SELECT pg.PageID,
           a.ActionID,
           CASE WHEN a.ActionCode = N'NAVIGATE' THEN pg.PageCode ELSE CONCAT(pg.PageCode, N'_', a.ActionCode) END AS PermissionCode
    FROM (
        VALUES
            (N'PAGE_ADMIN_USERS', N'NAVIGATE'), (N'PAGE_ADMIN_USERS', N'CREATE'), (N'PAGE_ADMIN_USERS', N'DELETE'), (N'PAGE_ADMIN_USERS', N'EDIT'), (N'PAGE_ADMIN_USERS', N'FIND'), (N'PAGE_ADMIN_USERS', N'FIRST'), (N'PAGE_ADMIN_USERS', N'PREVIOUS'), (N'PAGE_ADMIN_USERS', N'NEXT'), (N'PAGE_ADMIN_USERS', N'LAST'),
            (N'PAGE_ADMIN_PERMISSIONS', N'NAVIGATE'), (N'PAGE_ADMIN_PERMISSIONS', N'EDIT'), (N'PAGE_ADMIN_PERMISSIONS', N'FIND'), (N'PAGE_ADMIN_PERMISSIONS', N'FIRST'), (N'PAGE_ADMIN_PERMISSIONS', N'PREVIOUS'), (N'PAGE_ADMIN_PERMISSIONS', N'NEXT'), (N'PAGE_ADMIN_PERMISSIONS', N'LAST'),
            (N'PAGE_ADMIN_BRANDING', N'NAVIGATE'), (N'PAGE_ADMIN_BRANDING', N'EDIT'), (N'PAGE_ADMIN_BRANDING', N'FIND'),
            (N'PAGE_DASHBOARD', N'NAVIGATE'), (N'PAGE_DASHBOARD', N'FIND'), (N'PAGE_DASHBOARD', N'PRINT'), (N'PAGE_DASHBOARD', N'FIRST'), (N'PAGE_DASHBOARD', N'PREVIOUS'), (N'PAGE_DASHBOARD', N'NEXT'), (N'PAGE_DASHBOARD', N'LAST'),
            (N'PAGE_COMPONENT_TEST', N'NAVIGATE'), (N'PAGE_COMPONENT_TEST', N'FIND'), (N'PAGE_COMPONENT_TEST', N'PRINT'), (N'PAGE_COMPONENT_TEST', N'FIRST'), (N'PAGE_COMPONENT_TEST', N'PREVIOUS'), (N'PAGE_COMPONENT_TEST', N'NEXT'), (N'PAGE_COMPONENT_TEST', N'LAST'),
            (N'PAGE_CUSTOMER_LAYOUT_TEST', N'NAVIGATE'), (N'PAGE_CUSTOMER_LAYOUT_TEST', N'CREATE'), (N'PAGE_CUSTOMER_LAYOUT_TEST', N'DELETE'), (N'PAGE_CUSTOMER_LAYOUT_TEST', N'COPY'), (N'PAGE_CUSTOMER_LAYOUT_TEST', N'EDIT'), (N'PAGE_CUSTOMER_LAYOUT_TEST', N'CANCEL'), (N'PAGE_CUSTOMER_LAYOUT_TEST', N'FIND'), (N'PAGE_CUSTOMER_LAYOUT_TEST', N'PRINT'), (N'PAGE_CUSTOMER_LAYOUT_TEST', N'FIRST'), (N'PAGE_CUSTOMER_LAYOUT_TEST', N'PREVIOUS'), (N'PAGE_CUSTOMER_LAYOUT_TEST', N'NEXT'), (N'PAGE_CUSTOMER_LAYOUT_TEST', N'LAST'),
            (N'PAGE_PRODUCT_LAYOUT_TEST', N'NAVIGATE'), (N'PAGE_PRODUCT_LAYOUT_TEST', N'CREATE'), (N'PAGE_PRODUCT_LAYOUT_TEST', N'DELETE'), (N'PAGE_PRODUCT_LAYOUT_TEST', N'COPY'), (N'PAGE_PRODUCT_LAYOUT_TEST', N'EDIT'), (N'PAGE_PRODUCT_LAYOUT_TEST', N'CANCEL'), (N'PAGE_PRODUCT_LAYOUT_TEST', N'FIND'), (N'PAGE_PRODUCT_LAYOUT_TEST', N'PRINT'), (N'PAGE_PRODUCT_LAYOUT_TEST', N'FIRST'), (N'PAGE_PRODUCT_LAYOUT_TEST', N'PREVIOUS'), (N'PAGE_PRODUCT_LAYOUT_TEST', N'NEXT'), (N'PAGE_PRODUCT_LAYOUT_TEST', N'LAST'),
            (N'PAGE_PRODUCT_MATERIAL', N'NAVIGATE'), (N'PAGE_PRODUCT_MATERIAL', N'CREATE'), (N'PAGE_PRODUCT_MATERIAL', N'DELETE'), (N'PAGE_PRODUCT_MATERIAL', N'COPY'), (N'PAGE_PRODUCT_MATERIAL', N'EDIT'), (N'PAGE_PRODUCT_MATERIAL', N'CANCEL'), (N'PAGE_PRODUCT_MATERIAL', N'FIND'), (N'PAGE_PRODUCT_MATERIAL', N'PRINT'), (N'PAGE_PRODUCT_MATERIAL', N'FIRST'), (N'PAGE_PRODUCT_MATERIAL', N'PREVIOUS'), (N'PAGE_PRODUCT_MATERIAL', N'NEXT'), (N'PAGE_PRODUCT_MATERIAL', N'LAST'),
            (N'PAGE_SUPPLIER', N'NAVIGATE'), (N'PAGE_SUPPLIER', N'CREATE'), (N'PAGE_SUPPLIER', N'DELETE'), (N'PAGE_SUPPLIER', N'COPY'), (N'PAGE_SUPPLIER', N'EDIT'), (N'PAGE_SUPPLIER', N'CANCEL'), (N'PAGE_SUPPLIER', N'FIND'), (N'PAGE_SUPPLIER', N'PRINT'), (N'PAGE_SUPPLIER', N'FIRST'), (N'PAGE_SUPPLIER', N'PREVIOUS'), (N'PAGE_SUPPLIER', N'NEXT'), (N'PAGE_SUPPLIER', N'LAST')
    ) AS supported(PageCode, ActionCode)
    INNER JOIN dbo.AppPage pg ON pg.PageCode = supported.PageCode
    INNER JOIN dbo.AppAction a ON a.ActionCode = supported.ActionCode
) AS source (PageID, ActionID, PermissionCode)
ON target.PageID = source.PageID AND target.ActionID = source.ActionID
WHEN MATCHED THEN
    UPDATE SET PermissionCode = source.PermissionCode, IsActive = 1
WHEN NOT MATCHED THEN
    INSERT (PageID, ActionID, PermissionCode, IsActive)
    VALUES (source.PageID, source.ActionID, source.PermissionCode, 1);
GO

MERGE dbo.AppPermission AS target
USING (
    SELECT pa.PermissionCode,
           CASE WHEN a.ActionCode = N'NAVIGATE' THEN pg.PageName ELSE CONCAT(pg.PageName, N'-', a.ActionName) END AS PermissionName,
           CASE WHEN a.ActionCode = N'NAVIGATE' THEN N'PAGE' ELSE pg.PageCode END AS ModuleCode,
           a.ActionCode,
           CONCAT(N'允許', a.ActionName, N'「', pg.PageName, N'」。') AS Description
    FROM dbo.PageAction pa
    INNER JOIN dbo.AppPage pg ON pg.PageID = pa.PageID
    INNER JOIN dbo.AppAction a ON a.ActionID = pa.ActionID
) AS source (PermissionCode, PermissionName, ModuleCode, ActionCode, Description)
ON target.PermissionCode = source.PermissionCode
WHEN MATCHED THEN
    UPDATE SET PermissionName = source.PermissionName, ModuleCode = source.ModuleCode, ActionCode = source.ActionCode, Description = source.Description, IsActive = 1
WHEN NOT MATCHED THEN
    INSERT (PermissionCode, PermissionName, ModuleCode, ActionCode, Description, IsActive)
    VALUES (source.PermissionCode, source.PermissionName, source.ModuleCode, source.ActionCode, source.Description, 1);
GO

MERGE dbo.AppRole AS target
USING (
    VALUES
        (N'ADMIN', N'系統管理員', N'完整系統權限。', CAST(1 AS BIT), CAST(1 AS BIT)),
        (N'SALES', N'銷售', N'客戶與商品資料查詢及銷售常用維護。', CAST(0 AS BIT), CAST(1 AS BIT)),
        (N'PURCHASING', N'採購', N'商品與採購相關資料維護預留。', CAST(0 AS BIT), CAST(1 AS BIT)),
        (N'WAREHOUSE', N'倉管', N'庫存與出入庫相關資料維護預留。', CAST(0 AS BIT), CAST(1 AS BIT))
) AS source (RoleCode, RoleName, Description, IsSystemRole, IsActive)
ON target.RoleCode = source.RoleCode
WHEN MATCHED THEN
    UPDATE SET RoleName = source.RoleName, Description = source.Description, IsSystemRole = source.IsSystemRole, IsActive = source.IsActive
WHEN NOT MATCHED THEN
    INSERT (RoleCode, RoleName, Description, IsSystemRole, IsActive)
    VALUES (source.RoleCode, source.RoleName, source.Description, source.IsSystemRole, source.IsActive);
GO

INSERT INTO dbo.RolePageAction (RoleID, PageActionID)
SELECT r.RoleID, pa.PageActionID
FROM dbo.AppRole r
CROSS JOIN dbo.PageAction pa
WHERE r.RoleCode = N'ADMIN'
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.RolePageAction existing
      WHERE existing.RoleID = r.RoleID
        AND existing.PageActionID = pa.PageActionID
  );
GO

INSERT INTO dbo.RolePageAction (RoleID, PageActionID)
SELECT r.RoleID, pa.PageActionID
FROM dbo.AppRole r
INNER JOIN (
    VALUES
        (N'PAGE_CUSTOMER_LAYOUT_TEST'), (N'PAGE_CUSTOMER_LAYOUT_TEST_CREATE'), (N'PAGE_CUSTOMER_LAYOUT_TEST_EDIT'), (N'PAGE_CUSTOMER_LAYOUT_TEST_FIND'), (N'PAGE_CUSTOMER_LAYOUT_TEST_PRINT'), (N'PAGE_CUSTOMER_LAYOUT_TEST_FIRST'), (N'PAGE_CUSTOMER_LAYOUT_TEST_PREVIOUS'), (N'PAGE_CUSTOMER_LAYOUT_TEST_NEXT'), (N'PAGE_CUSTOMER_LAYOUT_TEST_LAST'),
        (N'PAGE_PRODUCT_LAYOUT_TEST'), (N'PAGE_PRODUCT_LAYOUT_TEST_FIND'), (N'PAGE_PRODUCT_LAYOUT_TEST_PRINT'), (N'PAGE_PRODUCT_LAYOUT_TEST_FIRST'), (N'PAGE_PRODUCT_LAYOUT_TEST_PREVIOUS'), (N'PAGE_PRODUCT_LAYOUT_TEST_NEXT'), (N'PAGE_PRODUCT_LAYOUT_TEST_LAST'),
        (N'PAGE_PRODUCT_MATERIAL'), (N'PAGE_PRODUCT_MATERIAL_FIND'), (N'PAGE_PRODUCT_MATERIAL_PRINT'), (N'PAGE_PRODUCT_MATERIAL_FIRST'), (N'PAGE_PRODUCT_MATERIAL_PREVIOUS'), (N'PAGE_PRODUCT_MATERIAL_NEXT'), (N'PAGE_PRODUCT_MATERIAL_LAST'),
        (N'PAGE_SUPPLIER'), (N'PAGE_SUPPLIER_FIND'), (N'PAGE_SUPPLIER_PRINT'), (N'PAGE_SUPPLIER_FIRST'), (N'PAGE_SUPPLIER_PREVIOUS'), (N'PAGE_SUPPLIER_NEXT'), (N'PAGE_SUPPLIER_LAST')
) AS allowed(PermissionCode) ON r.RoleCode = N'SALES'
INNER JOIN dbo.PageAction pa ON pa.PermissionCode = allowed.PermissionCode
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.RolePageAction existing
    WHERE existing.RoleID = r.RoleID
      AND existing.PageActionID = pa.PageActionID
);
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

INSERT INTO dbo.UserRole (AppUserID, RoleID)
SELECT u.AppUserID, r.RoleID
FROM dbo.AppUser u
INNER JOIN dbo.AppRole r ON r.RoleCode = N'ADMIN'
WHERE u.UserName = N'Admin'
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.UserRole existing
      WHERE existing.AppUserID = u.AppUserID
        AND existing.RoleID = r.RoleID
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

MERGE dbo.ProductMaterial AS target
USING (
    VALUES
        (N'MAT-001', N'鋁'),
        (N'MAT-002', N'不鏽鋼'),
        (N'MAT-003', N'鐵板'),
        (N'MAT-004', N'銅')
) AS source (MaterialCode, MaterialName)
ON target.MaterialCode = source.MaterialCode
WHEN MATCHED THEN
    UPDATE SET
        MaterialName = source.MaterialName,
        IsActive = 1,
        UpdatedAt = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
    INSERT (MaterialCode, MaterialName, IsActive)
    VALUES (source.MaterialCode, source.MaterialName, 1);
GO

PRINT N'SmartIMS database setup completed.';
GO
