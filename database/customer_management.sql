IF COL_LENGTH('dbo.Customer', 'Fax') IS NULL
    ALTER TABLE dbo.Customer ADD Fax NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.Customer', 'Email') IS NULL
    ALTER TABLE dbo.Customer ADD Email NVARCHAR(254) NULL;
IF COL_LENGTH('dbo.Customer', 'LineId') IS NULL
    ALTER TABLE dbo.Customer ADD LineId NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.Customer', 'ZipCode') IS NULL
    ALTER TABLE dbo.Customer ADD ZipCode NVARCHAR(20) NULL;
IF COL_LENGTH('dbo.Customer', 'PaymentMethod') IS NULL
    ALTER TABLE dbo.Customer ADD PaymentMethod NVARCHAR(20) NOT NULL DEFAULT (N'月結');
IF COL_LENGTH('dbo.Customer', 'BankAccount') IS NULL
    ALTER TABLE dbo.Customer ADD BankAccount NVARCHAR(100) NULL;
IF COL_LENGTH('dbo.Customer', 'Note') IS NULL
    ALTER TABLE dbo.Customer ADD Note NVARCHAR(2000) NULL;
IF COL_LENGTH('dbo.Customer', 'PaymentTermDays') IS NULL
    ALTER TABLE dbo.Customer ADD PaymentTermDays INT NOT NULL DEFAULT (30);

IF EXISTS (
    SELECT 1 FROM sys.extended_properties
    WHERE class = 1 AND major_id = OBJECT_ID(N'dbo.Customer')
      AND minor_id = COLUMNPROPERTY(OBJECT_ID(N'dbo.Customer'), N'CustomerName', 'ColumnId')
      AND name = N'MS_Description'
)
    EXEC sys.sp_updateextendedproperty @name=N'MS_Description', @value=N'公司名稱',
        @level0type=N'SCHEMA', @level0name=N'dbo',
        @level1type=N'TABLE', @level1name=N'Customer',
        @level2type=N'COLUMN', @level2name=N'CustomerName';
ELSE
    EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'公司名稱',
        @level0type=N'SCHEMA', @level0name=N'dbo',
        @level1type=N'TABLE', @level1name=N'Customer',
        @level2type=N'COLUMN', @level2name=N'CustomerName';
