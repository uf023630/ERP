-- Run in the SmartIMS database after creating Product and ProductMaterial.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
IF COL_LENGTH('dbo.Product', 'ProductMaterialID') IS NULL
    ALTER TABLE dbo.Product ADD ProductMaterialID BIGINT NULL;
IF COL_LENGTH('dbo.Product', 'Thickness') IS NULL
    ALTER TABLE dbo.Product ADD Thickness NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.Product', 'Length') IS NULL
    ALTER TABLE dbo.Product ADD Length NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.Product', 'Width') IS NULL
    ALTER TABLE dbo.Product ADD Width NVARCHAR(50) NULL;
IF COL_LENGTH('dbo.Product', 'Note') IS NULL
    ALTER TABLE dbo.Product ADD Note NVARCHAR(2000) NULL;
IF COL_LENGTH('dbo.Product', 'CreatedAt') IS NULL
    ALTER TABLE dbo.Product ADD CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Product_CreatedAt DEFAULT (SYSUTCDATETIME());
IF COL_LENGTH('dbo.Product', 'UpdatedAt') IS NULL
    ALTER TABLE dbo.Product ADD UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Product_UpdatedAt DEFAULT (SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Product_ProductMaterial')
    ALTER TABLE dbo.Product WITH CHECK ADD CONSTRAINT FK_Product_ProductMaterial
        FOREIGN KEY (ProductMaterialID) REFERENCES dbo.ProductMaterial(ProductMaterialID);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Product_Code_Active' AND object_id = OBJECT_ID('dbo.Product'))
    EXEC('CREATE UNIQUE INDEX UQ_Product_Code_Active ON dbo.Product(ProductCode) WHERE IsActive = 1;');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Product_ProductMaterial' AND object_id = OBJECT_ID('dbo.Product'))
    EXEC('CREATE INDEX IX_Product_ProductMaterial ON dbo.Product(ProductMaterialID, IsActive);');
