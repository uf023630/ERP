SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF COL_LENGTH('dbo.Customer', 'Address') IS NOT NULL
    BEGIN
        IF COL_LENGTH('dbo.Customer', 'CompanyAddress') IS NOT NULL
            THROW 50001, N'Customer 同時有 Address 與 CompanyAddress，請先確認資料，未自動覆蓋。', 1;
        EXEC sys.sp_rename N'dbo.Customer.Address', N'CompanyAddress', N'COLUMN';
    END;
    IF COL_LENGTH('dbo.Customer', 'MailingAddress') IS NOT NULL
    BEGIN
        IF COL_LENGTH('dbo.Customer', 'DeliveryAddress') IS NOT NULL
            THROW 50002, N'Customer 同時有 MailingAddress 與 DeliveryAddress，請先確認資料，未自動覆蓋。', 1;
        EXEC sys.sp_rename N'dbo.Customer.MailingAddress', N'DeliveryAddress', N'COLUMN';
    END;
    IF COL_LENGTH('dbo.Customer', 'CompanyAddress') IS NULL
        ALTER TABLE dbo.Customer ADD CompanyAddress NVARCHAR(500) NULL;
    IF COL_LENGTH('dbo.Customer', 'DeliveryAddress') IS NULL
        ALTER TABLE dbo.Customer ADD DeliveryAddress NVARCHAR(500) NULL;

    IF EXISTS (SELECT 1 FROM sys.extended_properties
               WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Customer')
                 AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Customer'), N'CompanyAddress', 'ColumnId')
                 AND name = N'MS_Description')
        EXEC sys.sp_updateextendedproperty @name=N'MS_Description', @value=N'公司地址',
            @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Customer',
            @level2type=N'COLUMN', @level2name=N'CompanyAddress';
    ELSE
        EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'公司地址',
            @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Customer',
            @level2type=N'COLUMN', @level2name=N'CompanyAddress';

    IF EXISTS (SELECT 1 FROM sys.extended_properties
               WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Customer')
                 AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Customer'), N'DeliveryAddress', 'ColumnId')
                 AND name = N'MS_Description')
        EXEC sys.sp_updateextendedproperty @name=N'MS_Description', @value=N'送貨地址',
            @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Customer',
            @level2type=N'COLUMN', @level2name=N'DeliveryAddress';
    ELSE
        EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'送貨地址',
            @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Customer',
            @level2type=N'COLUMN', @level2name=N'DeliveryAddress';
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
