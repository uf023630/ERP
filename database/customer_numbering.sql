-- Customer codes: C00001 through C99999. Run after the core schema exists.
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM dbo.NumberSequence WITH (UPDLOCK, HOLDLOCK) WHERE SequenceKey = N'CUSTOMER')
    INSERT INTO dbo.NumberSequence (SequenceKey, Prefix, CurrentValue, PaddingLength)
    VALUES (N'CUSTOMER', N'C', 0, 5);
DECLARE @Current BIGINT, @MaxCode BIGINT, @InvalidCount BIGINT, @CustomerCount BIGINT;
SELECT @Current = CurrentValue FROM dbo.NumberSequence WITH (UPDLOCK, HOLDLOCK) WHERE SequenceKey = N'CUSTOMER';
SELECT @CustomerCount = COUNT_BIG(*) FROM dbo.Customer WITH (TABLOCKX, HOLDLOCK);
SELECT @MaxCode = ISNULL(MAX(TRY_CONVERT(BIGINT, SUBSTRING(CustomerCode, 2, 5))), 0)
FROM dbo.Customer
WHERE DATALENGTH(CustomerCode) = 12
  AND CustomerCode COLLATE Latin1_General_100_BIN2 LIKE N'C[0-9][0-9][0-9][0-9][0-9]'
  AND TRY_CONVERT(BIGINT, SUBSTRING(CustomerCode, 2, 5)) BETWEEN 1 AND 99999;
SET @Current = CASE WHEN @Current > @MaxCode THEN @Current ELSE @MaxCode END;
DECLARE @Renumbered TABLE (CustomerID BIGINT PRIMARY KEY, OldCode NVARCHAR(50), NewCode NVARCHAR(50));
INSERT INTO @Renumbered (CustomerID, OldCode, NewCode)
SELECT CustomerID, CustomerCode,
       N'C' + RIGHT(N'00000' + CONVERT(NVARCHAR(20), @Current + ROW_NUMBER() OVER (ORDER BY CustomerID)), 5)
FROM dbo.Customer
WHERE NOT (DATALENGTH(CustomerCode) = 12
  AND CustomerCode COLLATE Latin1_General_100_BIN2 LIKE N'C[0-9][0-9][0-9][0-9][0-9]'
  AND TRY_CONVERT(BIGINT, SUBSTRING(CustomerCode, 2, 5)) BETWEEN 1 AND 99999);
SET @InvalidCount = @@ROWCOUNT;
IF @Current + @InvalidCount > 99999
    THROW 50001, N'客戶編號已超過 C99999，無法使用五碼流水號。', 1;
UPDATE c SET CustomerCode = r.NewCode FROM dbo.Customer c INNER JOIN @Renumbered r ON r.CustomerID = c.CustomerID;
UPDATE q SET CustomerCode = r.NewCode FROM dbo.SalesQuote q INNER JOIN @Renumbered r ON r.CustomerID = q.CustomerID;
UPDATE s SET CustomerCode = r.NewCode FROM dbo.SalesOrder s INNER JOIN @Renumbered r ON r.CustomerID = s.CustomerID;
UPDATE dbo.NumberSequence
SET Prefix = N'C', PaddingLength = 5, CurrentValue = @Current + @InvalidCount, UpdatedAt = SYSUTCDATETIME()
WHERE SequenceKey = N'CUSTOMER'
  AND (Prefix <> N'C' OR PaddingLength <> 5 OR CurrentValue <> @Current + @InvalidCount);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Customer') AND name = N'UQ_Customer_Code')
    CREATE UNIQUE INDEX UQ_Customer_Code ON dbo.Customer(CustomerCode);
COMMIT TRANSACTION;
