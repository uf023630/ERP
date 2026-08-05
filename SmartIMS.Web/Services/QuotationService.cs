using System.Data;
using Microsoft.Data.SqlClient;
using SmartIMS.Web.Infrastructure;
using SmartIMS.Web.ViewModels;

namespace SmartIMS.Web.Services;

public sealed class QuotationService
{
    private readonly SqlConnectionFactory _connectionFactory;

    public QuotationService(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<QuotationListViewModel> GetListAsync()
    {
        var items = new List<QuotationListItem>();
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT q.SalesQuoteID,
                   q.QuoteNo,
                   q.QuoteDate,
                   q.ExpireDate,
                   COALESCE(NULLIF(q.CustomerCode, N''), c.CustomerCode),
                   COALESCE(NULLIF(q.CustomerName, N''), c.CustomerName),
                   q.TaxType,
                   q.SubTotal,
                   q.TaxAmount,
                   q.TotalAmount,
                   q.Status,
                   so.SONo
            FROM dbo.SalesQuote q
            INNER JOIN dbo.Customer c ON c.CustomerID = q.CustomerID
            LEFT JOIN dbo.SalesOrder so ON so.SourceQuoteID = q.SalesQuoteID
            ORDER BY q.QuoteDate DESC, q.SalesQuoteID DESC;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new QuotationListItem(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetDateTime(2),
                reader.GetDateTime(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetDecimal(7),
                reader.GetDecimal(8),
                reader.GetDecimal(9),
                reader.GetString(10),
                reader.IsDBNull(11) ? null : reader.GetString(11)));
        }

        return new QuotationListViewModel { Quotations = items };
    }

    public async Task<QuotationEditViewModel> CreateEditModelAsync()
    {
        var model = new QuotationEditViewModel();
        model.Lines.Add(new QuotationLineInputModel());
        await PopulateLookupsAsync(model);
        return model;
    }

    public async Task<QuotationEditViewModel?> CreateCopyModelAsync(long salesQuoteId)
    {
        var model = await GetEditModelAsync(salesQuoteId);
        if (model is null)
        {
            return null;
        }

        var validityDays = Math.Max(0, (model.ExpireDate.Date - model.QuoteDate.Date).Days);
        model.SalesQuoteId = null;
        model.QuoteNo = null;
        model.QuoteDate = DateTime.Today;
        model.ExpireDate = DateTime.Today.AddDays(validityDays);
        model.Status = QuotationStatuses.Quoting;
        model.SalesOrderNo = null;

        foreach (var line in model.Lines)
        {
            line.SalesQuoteItemId = null;
        }

        return model;
    }

    public async Task<QuotationDetailViewModel?> GetDetailAsync(long salesQuoteId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        QuotationDetailViewModel? model;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT q.SalesQuoteID,
                       q.QuoteNo,
                       q.QuoteDate,
                       q.ExpireDate,
                       COALESCE(NULLIF(q.CustomerCode, N''), c.CustomerCode),
                       COALESCE(NULLIF(q.CustomerName, N''), c.CustomerName),
                       COALESCE(q.CustomerTaxId, c.TaxId),
                       COALESCE(q.CustomerContactName, c.ContactName),
                       COALESCE(q.CustomerPhone, c.Phone),
                       COALESCE(q.CustomerAddress, c.Address),
                       q.TaxType,
                       q.TaxRate,
                       q.SubTotal,
                       q.TaxAmount,
                       q.TotalAmount,
                       q.Status,
                       so.SONo,
                       q.Remark,
                       q.IsCustomerSuppliedMaterial
                FROM dbo.SalesQuote q
                INNER JOIN dbo.Customer c ON c.CustomerID = q.CustomerID
                LEFT JOIN dbo.SalesOrder so ON so.SourceQuoteID = q.SalesQuoteID
                WHERE q.SalesQuoteID = @SalesQuoteID;
                """;
            command.Parameters.AddWithValue("@SalesQuoteID", salesQuoteId);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            model = new QuotationDetailViewModel
            {
                SalesQuoteId = reader.GetInt64(0),
                QuoteNo = reader.GetString(1),
                QuoteDate = reader.GetDateTime(2),
                ExpireDate = reader.GetDateTime(3),
                CustomerCode = reader.GetString(4),
                CustomerName = reader.GetString(5),
                CustomerTaxId = reader.IsDBNull(6) ? null : reader.GetString(6),
                CustomerContactName = reader.IsDBNull(7) ? null : reader.GetString(7),
                CustomerPhone = reader.IsDBNull(8) ? null : reader.GetString(8),
                CustomerAddress = reader.IsDBNull(9) ? null : reader.GetString(9),
                TaxType = reader.GetString(10),
                TaxRate = reader.GetDecimal(11),
                Subtotal = reader.GetDecimal(12),
                TaxAmount = reader.GetDecimal(13),
                TotalAmount = reader.GetDecimal(14),
                Status = reader.GetString(15),
                SalesOrderNo = reader.IsDBNull(16) ? null : reader.GetString(16),
                Remark = reader.IsDBNull(17) ? null : reader.GetString(17),
                IsCustomerSuppliedMaterial = reader.GetBoolean(18)
            };
        }

        var lines = new List<QuotationDetailLine>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT LineNumber,
                       ProductID,
                       ItemSource,
                       ProductCode,
                       ProductName,
                       Specification,
                       Unit,
                       Qty,
                       UnitPrice,
                       LineAmount,
                       Remark
                FROM dbo.SalesQuoteItem
                WHERE SalesQuoteID = @SalesQuoteID
                ORDER BY LineNumber;
                """;
            command.Parameters.AddWithValue("@SalesQuoteID", salesQuoteId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lines.Add(new QuotationDetailLine(
                    reader.GetInt32(0),
                    reader.IsDBNull(1) ? null : reader.GetInt64(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.GetString(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6),
                    reader.GetDecimal(7),
                    reader.GetDecimal(8),
                    reader.GetDecimal(9),
                    reader.IsDBNull(10) ? null : reader.GetString(10)));
            }
        }

        model.Lines = lines;
        return model;
    }

    public async Task<QuotationEditViewModel?> GetEditModelAsync(long salesQuoteId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        QuotationEditViewModel? model;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT q.SalesQuoteID,
                       q.QuoteNo,
                       q.CustomerID,
                       q.QuoteDate,
                       q.ExpireDate,
                       q.TaxType,
                       q.TaxRate,
                       q.SubTotal,
                       q.TaxAmount,
                       q.TotalAmount,
                       q.Status,
                       q.Remark,
                       so.SONo,
                       q.IsCustomerSuppliedMaterial
                FROM dbo.SalesQuote q
                LEFT JOIN dbo.SalesOrder so ON so.SourceQuoteID = q.SalesQuoteID
                WHERE q.SalesQuoteID = @SalesQuoteID;
                """;
            command.Parameters.AddWithValue("@SalesQuoteID", salesQuoteId);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            model = new QuotationEditViewModel
            {
                SalesQuoteId = reader.GetInt64(0),
                QuoteNo = reader.GetString(1),
                CustomerId = reader.GetInt64(2),
                QuoteDate = reader.GetDateTime(3),
                ExpireDate = reader.GetDateTime(4),
                TaxType = reader.GetString(5),
                TaxRate = reader.GetDecimal(6),
                Subtotal = reader.GetDecimal(7),
                TaxAmount = reader.GetDecimal(8),
                TotalAmount = reader.GetDecimal(9),
                Status = reader.GetString(10),
                Remark = reader.IsDBNull(11) ? null : reader.GetString(11),
                SalesOrderNo = reader.IsDBNull(12) ? null : reader.GetString(12),
                IsCustomerSuppliedMaterial = reader.GetBoolean(13)
            };
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT SalesQuoteItemID,
                       ProductID,
                       ItemSource,
                       ProductCode,
                       ProductName,
                       Specification,
                       Unit,
                       Qty,
                       UnitPrice,
                       LineAmount,
                       Remark
                FROM dbo.SalesQuoteItem
                WHERE SalesQuoteID = @SalesQuoteID
                ORDER BY LineNumber;
                """;
            command.Parameters.AddWithValue("@SalesQuoteID", salesQuoteId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                model.Lines.Add(new QuotationLineInputModel
                {
                    SalesQuoteItemId = reader.GetInt64(0),
                    ProductId = reader.IsDBNull(1) ? null : reader.GetInt64(1),
                    ItemSource = reader.GetString(2),
                    ProductCode = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ProductName = reader.GetString(4),
                    Specification = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Unit = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Quantity = reader.GetDecimal(7),
                    UnitPrice = reader.GetDecimal(8),
                    LineAmount = reader.GetDecimal(9),
                    Remark = reader.IsDBNull(10) ? null : reader.GetString(10)
                });
            }
        }

        await PopulateLookupsAsync(model);
        return model;
    }

    public async Task PopulateLookupsAsync(QuotationEditViewModel model)
    {
        var customers = new List<QuotationCustomerOption>();
        var products = new List<QuotationProductOption>();

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CustomerID, CustomerCode, CustomerName, TaxId, ContactName, Phone, Address, TaxType
            FROM dbo.Customer
            WHERE IsActive = 1
            ORDER BY CustomerCode, CustomerName;

            SELECT ProductID, ProductCode, ProductName, Specification, Unit, UnitPrice
            FROM dbo.Product
            WHERE IsActive = 1
            ORDER BY ProductCode, ProductName;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            customers.Add(new QuotationCustomerOption(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7)));
        }

        await reader.NextResultAsync();
        while (await reader.ReadAsync())
        {
            products.Add(new QuotationProductOption(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetDecimal(5)));
        }

        model.Customers = customers;
        model.Products = products;
    }

    public async Task<long> SaveAsync(QuotationEditViewModel model)
    {
        ValidateRequest(model);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var customer = await GetCustomerAsync(connection, transaction, model.CustomerId!.Value)
                ?? throw new InvalidOperationException("找不到選取的客戶，或該客戶已停用。");

            var normalizedLines = new List<NormalizedQuotationLine>();
            foreach (var input in model.Lines)
            {
                normalizedLines.Add(await NormalizeLineAsync(connection, transaction, input));
            }

            var totals = CalculateTotals(normalizedLines, model.TaxType, model.TaxRate);
            var salesQuoteId = model.SalesQuoteId;

            if (salesQuoteId.HasValue)
            {
                await using var update = connection.CreateCommand();
                update.Transaction = transaction;
                update.CommandText = """
                    UPDATE dbo.SalesQuote
                    SET CustomerID = @CustomerID,
                        CustomerCode = @CustomerCode,
                        CustomerName = @CustomerName,
                        CustomerTaxId = @CustomerTaxId,
                        CustomerContactName = @CustomerContactName,
                        CustomerPhone = @CustomerPhone,
                        CustomerAddress = @CustomerAddress,
                        QuoteDate = @QuoteDate,
                        ExpireDate = @ExpireDate,
                        TaxType = @TaxType,
                        TaxRate = @TaxRate,
                        SubTotal = @SubTotal,
                        TaxAmount = @TaxAmount,
                        TotalAmount = @TotalAmount,
                        IsCustomerSuppliedMaterial = @IsCustomerSuppliedMaterial,
                        Remark = @Remark,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE SalesQuoteID = @SalesQuoteID
                      AND Status = 'Quoting';
                    """;
                AddHeaderParameters(update, model, customer, totals);
                update.Parameters.AddWithValue("@SalesQuoteID", salesQuoteId.Value);
                if (await update.ExecuteNonQueryAsync() == 0)
                {
                    throw new InvalidOperationException("只有「報價中」的報價單可以修改。");
                }

                await using var deleteItems = connection.CreateCommand();
                deleteItems.Transaction = transaction;
                deleteItems.CommandText = "DELETE FROM dbo.SalesQuoteItem WHERE SalesQuoteID = @SalesQuoteID;";
                deleteItems.Parameters.AddWithValue("@SalesQuoteID", salesQuoteId.Value);
                await deleteItems.ExecuteNonQueryAsync();
            }
            else
            {
                var quoteNo = await GetNextNumberAsync(connection, transaction, "SALES_QUOTE");
                await using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO dbo.SalesQuote (
                        QuoteNo, CustomerID, CustomerCode, CustomerName, CustomerTaxId,
                        CustomerContactName, CustomerPhone, CustomerAddress,
                        QuoteDate, ExpireDate, Status, TaxType, TaxRate,
                        SubTotal, TaxAmount, TotalAmount, IsCustomerSuppliedMaterial,
                        Remark, IsLocked, UpdatedAt
                    )
                    OUTPUT inserted.SalesQuoteID
                    VALUES (
                        @QuoteNo, @CustomerID, @CustomerCode, @CustomerName, @CustomerTaxId,
                        @CustomerContactName, @CustomerPhone, @CustomerAddress,
                        @QuoteDate, @ExpireDate, 'Quoting', @TaxType, @TaxRate,
                        @SubTotal, @TaxAmount, @TotalAmount, @IsCustomerSuppliedMaterial,
                        @Remark, 0, SYSUTCDATETIME()
                    );
                    """;
                insert.Parameters.AddWithValue("@QuoteNo", quoteNo);
                AddHeaderParameters(insert, model, customer, totals);
                salesQuoteId = Convert.ToInt64(await insert.ExecuteScalarAsync());
            }

            for (var index = 0; index < normalizedLines.Count; index++)
            {
                await InsertQuoteLineAsync(connection, transaction, salesQuoteId!.Value, index + 1, normalizedLines[index], model.TaxRate);
            }

            await transaction.CommitAsync();
            return salesQuoteId!.Value;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task VoidAsync(long salesQuoteId, string? reason)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.SalesQuote
            SET Status = 'Voided',
                IsLocked = 1,
                VoidReason = @VoidReason,
                VoidedAt = SYSUTCDATETIME(),
                UpdatedAt = SYSUTCDATETIME()
            WHERE SalesQuoteID = @SalesQuoteID
              AND Status = 'Quoting';
            """;
        command.Parameters.AddWithValue("@SalesQuoteID", salesQuoteId);
        command.Parameters.AddWithValue("@VoidReason", DbValue(reason));

        if (await command.ExecuteNonQueryAsync() == 0)
        {
            throw new InvalidOperationException("只有「報價中」的報價單可以作廢。");
        }
    }

    public async Task<QuotationConversionResult> ConvertToSalesOrderAsync(long salesQuoteId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            QuoteForConversion quote;
            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = """
                    SELECT q.CustomerID, q.QuoteDate, q.ExpireDate, q.SubTotal, q.TaxAmount,
                           q.TotalAmount, q.Remark, q.TaxType, q.TaxRate,
                           q.CustomerCode, q.CustomerName, q.CustomerTaxId,
                           q.CustomerContactName, q.CustomerPhone, q.CustomerAddress,
                            c.PaymentTermDays, q.Status, q.IsCustomerSuppliedMaterial
                    FROM dbo.SalesQuote q WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN dbo.Customer c ON c.CustomerID = q.CustomerID
                    WHERE q.SalesQuoteID = @SalesQuoteID;
                    """;
                command.Parameters.AddWithValue("@SalesQuoteID", salesQuoteId);

                await using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    throw new InvalidOperationException("找不到報價單。");
                }

                var status = reader.GetString(16);
                if (status != QuotationStatuses.Quoting)
                {
                    throw new InvalidOperationException("只有「報價中」的報價單可以轉成銷售單。");
                }

                quote = new QuoteForConversion(
                    reader.GetInt64(0),
                    reader.GetDateTime(1),
                    reader.GetDateTime(2),
                    reader.GetDecimal(3),
                    reader.GetDecimal(4),
                    reader.GetDecimal(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6),
                    reader.GetString(7),
                    reader.GetDecimal(8),
                    reader.GetString(9),
                    reader.GetString(10),
                    reader.IsDBNull(11) ? null : reader.GetString(11),
                    reader.IsDBNull(12) ? null : reader.GetString(12),
                    reader.IsDBNull(13) ? null : reader.GetString(13),
                    reader.IsDBNull(14) ? null : reader.GetString(14),
                    reader.GetInt32(15),
                    reader.GetBoolean(17));
            }

            var salesOrderNo = await GetNextNumberAsync(connection, transaction, "SALES_ORDER");
            long salesOrderId;
            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = """
                    INSERT INTO dbo.SalesOrder (
                        SONo, CustomerID, SourceQuoteID, OrderDate, RequiredDate,
                        PaymentTermDays, Status, TaxType, TaxRate,
                        SubTotal, TaxAmount, TotalAmount, Remark,
                        IsCustomerSuppliedMaterial,
                        CustomerCode, CustomerName, CustomerTaxId,
                        CustomerContactName, CustomerPhone, CustomerAddress
                    )
                    OUTPUT inserted.SalesOrderID
                    VALUES (
                        @SONo, @CustomerID, @SourceQuoteID, @OrderDate, @RequiredDate,
                        @PaymentTermDays, 'Confirmed', @TaxType, @TaxRate,
                        @SubTotal, @TaxAmount, @TotalAmount, @Remark,
                        @IsCustomerSuppliedMaterial,
                        @CustomerCode, @CustomerName, @CustomerTaxId,
                        @CustomerContactName, @CustomerPhone, @CustomerAddress
                    );
                    """;
                command.Parameters.AddWithValue("@SONo", salesOrderNo);
                command.Parameters.AddWithValue("@CustomerID", quote.CustomerId);
                command.Parameters.AddWithValue("@SourceQuoteID", salesQuoteId);
                command.Parameters.AddWithValue("@OrderDate", quote.QuoteDate.Date);
                command.Parameters.AddWithValue("@RequiredDate", quote.ExpireDate.Date);
                command.Parameters.AddWithValue("@PaymentTermDays", quote.PaymentTermDays);
                command.Parameters.AddWithValue("@TaxType", quote.TaxType);
                command.Parameters.AddWithValue("@TaxRate", quote.TaxRate);
                command.Parameters.AddWithValue("@SubTotal", quote.Subtotal);
                command.Parameters.AddWithValue("@TaxAmount", quote.TaxAmount);
                command.Parameters.AddWithValue("@TotalAmount", quote.TotalAmount);
                command.Parameters.AddWithValue("@Remark", DbValue(quote.Remark));
                command.Parameters.AddWithValue("@IsCustomerSuppliedMaterial", quote.IsCustomerSuppliedMaterial);
                command.Parameters.AddWithValue("@CustomerCode", quote.CustomerCode);
                command.Parameters.AddWithValue("@CustomerName", quote.CustomerName);
                command.Parameters.AddWithValue("@CustomerTaxId", DbValue(quote.CustomerTaxId));
                command.Parameters.AddWithValue("@CustomerContactName", DbValue(quote.CustomerContactName));
                command.Parameters.AddWithValue("@CustomerPhone", DbValue(quote.CustomerPhone));
                command.Parameters.AddWithValue("@CustomerAddress", DbValue(quote.CustomerAddress));
                salesOrderId = Convert.ToInt64(await command.ExecuteScalarAsync());
            }

            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = """
                    INSERT INTO dbo.SalesOrderItem (
                        SalesOrderID, LineNumber, ProductID, ItemSource,
                        ProductCode, ProductName, Specification, Unit,
                        OrderedQty, ShippedQty, UnitPrice, TaxRate, Remark
                    )
                    SELECT @SalesOrderID, LineNumber, ProductID, ItemSource,
                           ProductCode, ProductName, Specification, Unit,
                           Qty, 0, UnitPrice, TaxRate, Remark
                    FROM dbo.SalesQuoteItem
                    WHERE SalesQuoteID = @SalesQuoteID;
                    """;
                command.Parameters.AddWithValue("@SalesOrderID", salesOrderId);
                command.Parameters.AddWithValue("@SalesQuoteID", salesQuoteId);
                await command.ExecuteNonQueryAsync();
            }

            await using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = """
                    UPDATE dbo.SalesQuote
                    SET Status = 'Converted',
                        IsLocked = 1,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE SalesQuoteID = @SalesQuoteID;
                    """;
                command.Parameters.AddWithValue("@SalesQuoteID", salesQuoteId);
                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
            return new QuotationConversionResult(salesOrderId, salesOrderNo);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static void ValidateRequest(QuotationEditViewModel model)
    {
        if (!model.CustomerId.HasValue)
        {
            throw new InvalidOperationException("請選擇客戶。");
        }

        if (model.ExpireDate.Date < model.QuoteDate.Date)
        {
            throw new InvalidOperationException("有效日期不可早於報價日期。");
        }

        if (model.TaxType is not (QuotationTaxTypes.Inclusive or QuotationTaxTypes.Exclusive))
        {
            throw new InvalidOperationException("計稅方式不正確。");
        }

        if (model.TaxRate is < 0 or > 100)
        {
            throw new InvalidOperationException("稅率必須介於 0 到 100。");
        }

        if (model.Lines.Count == 0)
        {
            throw new InvalidOperationException("請至少加入一項商品。");
        }
    }

    private static async Task<CustomerSnapshot?> GetCustomerAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long customerId)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT CustomerCode, CustomerName, TaxId, ContactName, Phone, Address
            FROM dbo.Customer
            WHERE CustomerID = @CustomerID
              AND IsActive = 1;
            """;
        command.Parameters.AddWithValue("@CustomerID", customerId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new CustomerSnapshot(
            customerId,
            reader.GetString(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetString(5));
    }

    private static async Task<NormalizedQuotationLine> NormalizeLineAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        QuotationLineInputModel input)
    {
        if (input.Quantity <= 0)
        {
            throw new InvalidOperationException("商品數量必須大於 0。");
        }

        if (input.UnitPrice < 0)
        {
            throw new InvalidOperationException("商品單價不可小於 0。");
        }

        string? defaultCode = null;
        string? defaultName = null;
        string? defaultSpecification = null;
        string? defaultUnit = null;
        if (input.ProductId.HasValue)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                SELECT ProductCode, ProductName, Specification, Unit
                FROM dbo.Product
                WHERE ProductID = @ProductID
                  AND IsActive = 1;
                """;
            command.Parameters.AddWithValue("@ProductID", input.ProductId.Value);
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException("選取的商品不存在或已停用。");
            }

            defaultCode = reader.GetString(0);
            defaultName = reader.GetString(1);
            defaultSpecification = reader.IsDBNull(2) ? null : reader.GetString(2);
            defaultUnit = reader.IsDBNull(3) ? null : reader.GetString(3);
        }

        var productName = FirstNonEmpty(input.ProductName, defaultName);
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new InvalidOperationException("請輸入商品名稱。");
        }

        var lineAmount = RoundMoney(input.Quantity * input.UnitPrice);
        return new NormalizedQuotationLine(
            input.ProductId,
            input.ProductId.HasValue ? "Product" : "Manual",
            FirstNonEmpty(input.ProductCode, defaultCode),
            productName,
            FirstNonEmpty(input.Specification, defaultSpecification),
            FirstNonEmpty(input.Unit, defaultUnit),
            input.Quantity,
            input.UnitPrice,
            lineAmount,
            NullIfWhiteSpace(input.Remark));
    }

    private static QuotationTotals CalculateTotals(
        IReadOnlyList<NormalizedQuotationLine> lines,
        string taxType,
        decimal taxRate)
    {
        var lineTotal = RoundMoney(lines.Sum(line => line.LineAmount));
        if (taxType == QuotationTaxTypes.Inclusive)
        {
            var tax = taxRate == 0 ? 0 : RoundMoney(lineTotal * taxRate / (100m + taxRate));
            return new QuotationTotals(lineTotal - tax, tax, lineTotal);
        }

        var exclusiveTax = RoundMoney(lineTotal * taxRate / 100m);
        return new QuotationTotals(lineTotal, exclusiveTax, lineTotal + exclusiveTax);
    }

    private static void AddHeaderParameters(
        SqlCommand command,
        QuotationEditViewModel model,
        CustomerSnapshot customer,
        QuotationTotals totals)
    {
        command.Parameters.AddWithValue("@CustomerID", customer.CustomerId);
        command.Parameters.AddWithValue("@CustomerCode", customer.CustomerCode);
        command.Parameters.AddWithValue("@CustomerName", customer.CustomerName);
        command.Parameters.AddWithValue("@CustomerTaxId", DbValue(customer.TaxId));
        command.Parameters.AddWithValue("@CustomerContactName", DbValue(customer.ContactName));
        command.Parameters.AddWithValue("@CustomerPhone", DbValue(customer.Phone));
        command.Parameters.AddWithValue("@CustomerAddress", DbValue(customer.Address));
        command.Parameters.AddWithValue("@QuoteDate", model.QuoteDate.Date);
        command.Parameters.AddWithValue("@ExpireDate", model.ExpireDate.Date);
        command.Parameters.AddWithValue("@TaxType", model.TaxType);
        command.Parameters.AddWithValue("@TaxRate", model.TaxRate);
        command.Parameters.AddWithValue("@IsCustomerSuppliedMaterial", model.IsCustomerSuppliedMaterial);
        command.Parameters.AddWithValue("@SubTotal", totals.Subtotal);
        command.Parameters.AddWithValue("@TaxAmount", totals.TaxAmount);
        command.Parameters.AddWithValue("@TotalAmount", totals.TotalAmount);
        command.Parameters.AddWithValue("@Remark", DbValue(model.Remark));
    }

    private static async Task InsertQuoteLineAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long salesQuoteId,
        int lineNumber,
        NormalizedQuotationLine line,
        decimal taxRate)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO dbo.SalesQuoteItem (
                SalesQuoteID, LineNumber, ProductID, ItemSource,
                ProductCode, ProductName, Specification, Unit,
                Qty, UnitPrice, TaxRate, Remark
            )
            VALUES (
                @SalesQuoteID, @LineNumber, @ProductID, @ItemSource,
                @ProductCode, @ProductName, @Specification, @Unit,
                @Qty, @UnitPrice, @TaxRate, @Remark
            );
            """;
        command.Parameters.AddWithValue("@SalesQuoteID", salesQuoteId);
        command.Parameters.AddWithValue("@LineNumber", lineNumber);
        command.Parameters.AddWithValue("@ProductID", DbValue(line.ProductId));
        command.Parameters.AddWithValue("@ItemSource", line.ItemSource);
        command.Parameters.AddWithValue("@ProductCode", DbValue(line.ProductCode));
        command.Parameters.AddWithValue("@ProductName", line.ProductName);
        command.Parameters.AddWithValue("@Specification", DbValue(line.Specification));
        command.Parameters.AddWithValue("@Unit", DbValue(line.Unit));
        command.Parameters.AddWithValue("@Qty", line.Quantity);
        command.Parameters.AddWithValue("@UnitPrice", line.UnitPrice);
        command.Parameters.AddWithValue("@TaxRate", taxRate);
        command.Parameters.AddWithValue("@Remark", DbValue(line.Remark));
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string> GetNextNumberAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string sequenceKey)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            UPDATE dbo.NumberSequence WITH (UPDLOCK, HOLDLOCK)
            SET CurrentValue = CurrentValue + 1,
                UpdatedAt = SYSUTCDATETIME()
            OUTPUT inserted.Prefix, inserted.CurrentValue, inserted.PaddingLength
            WHERE SequenceKey = @SequenceKey;
            """;
        command.Parameters.AddWithValue("@SequenceKey", sequenceKey);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException($"找不到單號序號設定：{sequenceKey}");
        }

        var prefix = reader.GetString(0);
        var value = reader.GetInt64(1);
        var padding = reader.GetInt32(2);
        return prefix + value.ToString($"D{padding}");
    }

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static object DbValue(object? value) => value ?? DBNull.Value;

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? FirstNonEmpty(string? preferred, string? fallback) =>
        !string.IsNullOrWhiteSpace(preferred) ? preferred.Trim() : NullIfWhiteSpace(fallback);

    private sealed record CustomerSnapshot(
        long CustomerId,
        string CustomerCode,
        string CustomerName,
        string? TaxId,
        string? ContactName,
        string? Phone,
        string? Address);

    private sealed record NormalizedQuotationLine(
        long? ProductId,
        string ItemSource,
        string? ProductCode,
        string ProductName,
        string? Specification,
        string? Unit,
        decimal Quantity,
        decimal UnitPrice,
        decimal LineAmount,
        string? Remark);

    private sealed record QuotationTotals(
        decimal Subtotal,
        decimal TaxAmount,
        decimal TotalAmount);

    private sealed record QuoteForConversion(
        long CustomerId,
        DateTime QuoteDate,
        DateTime ExpireDate,
        decimal Subtotal,
        decimal TaxAmount,
        decimal TotalAmount,
        string? Remark,
        string TaxType,
        decimal TaxRate,
        string CustomerCode,
        string CustomerName,
        string? CustomerTaxId,
        string? CustomerContactName,
        string? CustomerPhone,
        string? CustomerAddress,
        int PaymentTermDays,
        bool IsCustomerSuppliedMaterial);
}
