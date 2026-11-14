using System.Data;
using Microsoft.Data.SqlClient;
using SmartIMS.Web.Infrastructure;
using SmartIMS.Web.ViewModels;

namespace SmartIMS.Web.Services;

public sealed class SalesOrderService
{
    private readonly SqlConnectionFactory _connectionFactory;

    public SalesOrderService(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<SalesOrderListViewModel> GetListAsync()
    {
        var items = new List<SalesOrderListItem>();
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT q.SalesOrderID,
                   q.SONo,
                   q.OrderDate,
                   q.RequiredDate,
                   COALESCE(NULLIF(q.CustomerCode, N''), c.CustomerCode),
                   COALESCE(NULLIF(q.CustomerName, N''), c.CustomerName),
                   q.TaxType,
                   q.SubTotal,
                   q.TaxAmount,
                   q.TotalAmount,
                   q.Status,
                   sq.QuoteNo,
                   productNames.Names
            FROM dbo.SalesOrder q
            INNER JOIN dbo.Customer c ON c.CustomerID = q.CustomerID
            LEFT JOIN dbo.SalesQuote sq ON sq.SalesQuoteID = q.SourceQuoteID
            OUTER APPLY (
                SELECT STRING_AGG(CONVERT(NVARCHAR(MAX), qi.ProductName), N' | ') AS Names
                FROM dbo.SalesOrderItem qi
                WHERE qi.SalesOrderID = q.SalesOrderID
            ) productNames
            ORDER BY q.OrderDate DESC, q.SalesOrderID DESC;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new SalesOrderListItem(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetDateTime(2),
                reader.IsDBNull(3) ? reader.GetDateTime(2) : reader.GetDateTime(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetDecimal(7),
                reader.GetDecimal(8),
                reader.GetDecimal(9),
                reader.GetString(10),
                reader.IsDBNull(11) ? null : reader.GetString(11),
                reader.IsDBNull(12) ? "" : reader.GetString(12)));
        }

        return new SalesOrderListViewModel { SalesOrders = items };
    }

    public async Task<SalesOrderEditViewModel> CreateEditModelAsync()
    {
        var model = new SalesOrderEditViewModel();
        model.Lines.Add(new SalesOrderLineInputModel());
        await PopulateLookupsAsync(model);
        return model;
    }

    public async Task<SalesOrderEditViewModel?> CreateCopyModelAsync(long salesOrderId)
    {
        var model = await GetEditModelAsync(salesOrderId);
        if (model is null)
        {
            return null;
        }

        var validityDays = Math.Max(0, (model.RequiredDate.Date - model.OrderDate.Date).Days);
        model.SalesOrderId = null;
        model.OrderNo = null;
        model.OrderDate = DateTime.Today;
        model.RequiredDate = DateTime.Today.AddDays(validityDays);
        model.Status = SalesOrderStatuses.Confirmed;
        model.SourceQuoteNo = null;
        model.PdfFileName = null;
        model.PdfStoragePath = null;
        model.HasShippedItems = false;

        foreach (var line in model.Lines)
        {
            line.SalesOrderItemId = null;
        }

        return model;
    }

    public async Task<SalesOrderDetailViewModel?> GetDetailAsync(long salesOrderId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        SalesOrderDetailViewModel? model;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT q.SalesOrderID,
                       q.SONo,
                       q.OrderDate,
                       q.RequiredDate,
                       COALESCE(NULLIF(q.CustomerCode, N''), c.CustomerCode),
                       COALESCE(NULLIF(q.CustomerName, N''), c.CustomerName),
                       COALESCE(q.CustomerTaxId, c.TaxId),
                       COALESCE(q.CustomerContactName, c.ContactName),
                       COALESCE(q.CustomerPhone, c.Phone),
                       COALESCE(q.CustomerAddress, c.DeliveryAddress),
                       q.TaxType,
                       q.TaxRate,
                       q.SubTotal,
                       q.TaxAmount,
                       q.TotalAmount,
                       q.Status,
                       sq.QuoteNo,
                       q.Remark,
                       q.IsCustomerSuppliedMaterial, q.PdfFileName, q.PdfStoragePath,
                       CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.SalesOrderItem WHERE SalesOrderID = q.SalesOrderID AND ShippedQty > 0) THEN 1 ELSE 0 END AS BIT)
                FROM dbo.SalesOrder q
                INNER JOIN dbo.Customer c ON c.CustomerID = q.CustomerID
                LEFT JOIN dbo.SalesQuote sq ON sq.SalesQuoteID = q.SourceQuoteID
                WHERE q.SalesOrderID = @SalesOrderID;
                """;
            command.Parameters.AddWithValue("@SalesOrderID", salesOrderId);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            model = new SalesOrderDetailViewModel
            {
                SalesOrderId = reader.GetInt64(0),
                OrderNo = reader.GetString(1),
                OrderDate = reader.GetDateTime(2),
                RequiredDate = reader.IsDBNull(3) ? reader.GetDateTime(2) : reader.GetDateTime(3),
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
                SourceQuoteNo = reader.IsDBNull(16) ? null : reader.GetString(16),
                Remark = reader.IsDBNull(17) ? null : reader.GetString(17),
                IsCustomerSuppliedMaterial = reader.GetBoolean(18),
                PdfFileName = reader.IsDBNull(19) ? null : reader.GetString(19),
                PdfStoragePath = reader.IsDBNull(20) ? null : reader.GetString(20),
                HasShippedItems = reader.GetBoolean(21)
            };
        }

        var lines = new List<SalesOrderDetailLine>();
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
                       OrderedQty,
                       UnitPrice,
                       LineAmount,
                       Remark, Thickness, Width, Length
                FROM dbo.SalesOrderItem
                WHERE SalesOrderID = @SalesOrderID
                ORDER BY LineNumber;
                """;
            command.Parameters.AddWithValue("@SalesOrderID", salesOrderId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                lines.Add(new SalesOrderDetailLine(
                    reader.GetInt32(0),
                    reader.IsDBNull(1) ? null : reader.GetInt64(1),
                    reader.GetString(2),
                    reader.IsDBNull(3) ? null : reader.GetString(3),
                    reader.GetString(4),
                    reader.IsDBNull(11) ? null : reader.GetString(11),
                    reader.IsDBNull(12) ? null : reader.GetString(12),
                    reader.IsDBNull(13) ? null : reader.GetString(13),
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

    public async Task<SalesOrderEditViewModel?> GetEditModelAsync(long salesOrderId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        SalesOrderEditViewModel? model;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT q.SalesOrderID,
                       q.SONo,
                       q.CustomerID,
                       q.OrderDate,
                       q.RequiredDate,
                       q.TaxType,
                       q.TaxRate,
                       q.SubTotal,
                       q.TaxAmount,
                       q.TotalAmount,
                       q.Status,
                       q.Remark,
                       sq.QuoteNo,
                       q.IsCustomerSuppliedMaterial, q.PdfFileName, q.PdfStoragePath
                FROM dbo.SalesOrder q
                LEFT JOIN dbo.SalesQuote sq ON sq.SalesQuoteID = q.SourceQuoteID
                WHERE q.SalesOrderID = @SalesOrderID;
                """;
            command.Parameters.AddWithValue("@SalesOrderID", salesOrderId);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            model = new SalesOrderEditViewModel
            {
                SalesOrderId = reader.GetInt64(0),
                OrderNo = reader.GetString(1),
                CustomerId = reader.GetInt64(2),
                OrderDate = reader.IsDBNull(3) ? reader.GetDateTime(2) : reader.GetDateTime(3),
                RequiredDate = reader.IsDBNull(4) ? reader.GetDateTime(3) : reader.GetDateTime(4),
                TaxType = reader.GetString(5),
                TaxRate = reader.GetDecimal(6),
                Subtotal = reader.GetDecimal(7),
                TaxAmount = reader.GetDecimal(8),
                TotalAmount = reader.GetDecimal(9),
                Status = reader.GetString(10),
                Remark = reader.IsDBNull(11) ? null : reader.GetString(11),
                SourceQuoteNo = reader.IsDBNull(12) ? null : reader.GetString(12),
                IsCustomerSuppliedMaterial = reader.GetBoolean(13),
                PdfFileName = reader.IsDBNull(14) ? null : reader.GetString(14),
                PdfStoragePath = reader.IsDBNull(15) ? null : reader.GetString(15)
            };
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT SalesOrderItemID,
                       ProductID,
                       ItemSource,
                       ProductCode,
                       ProductName,
                       Specification,
                       Unit,
                       OrderedQty,
                       UnitPrice,
                       LineAmount,
                       Remark, Thickness, Width, Length
                FROM dbo.SalesOrderItem
                WHERE SalesOrderID = @SalesOrderID
                ORDER BY LineNumber;
                """;
            command.Parameters.AddWithValue("@SalesOrderID", salesOrderId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                model.Lines.Add(new SalesOrderLineInputModel
                {
                    SalesOrderItemId = reader.GetInt64(0),
                    ProductId = reader.IsDBNull(1) ? null : reader.GetInt64(1),
                    ItemSource = reader.GetString(2),
                    ProductCode = reader.IsDBNull(3) ? null : reader.GetString(3),
                    ProductName = reader.GetString(4),
                    Thickness = reader.IsDBNull(11) ? null : reader.GetString(11),
                    Width = reader.IsDBNull(12) ? null : reader.GetString(12),
                    Length = reader.IsDBNull(13) ? null : reader.GetString(13),
                    Specification = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Unit = reader.IsDBNull(6) ? null : reader.GetString(6),
                    Quantity = reader.GetDecimal(7),
                    UnitPrice = reader.GetDecimal(8),
                    LineAmount = reader.GetDecimal(9),
                    Remark = reader.IsDBNull(10) ? null : reader.GetString(10)
                });
            }
        }

        await using (var shipped = connection.CreateCommand())
        {
            shipped.CommandText = "SELECT COUNT(*) FROM dbo.SalesOrderItem WHERE SalesOrderID = @Id AND ShippedQty > 0;";
            shipped.Parameters.AddWithValue("@Id", salesOrderId);
            model.HasShippedItems = Convert.ToInt32(await shipped.ExecuteScalarAsync()) > 0;
        }
        await PopulateLookupsAsync(model);
        return model;
    }

    public async Task PopulateLookupsAsync(SalesOrderEditViewModel model)
    {
        var customers = new List<SalesOrderCustomerOption>();
        var products = new List<SalesOrderProductOption>();

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CustomerID, CustomerCode, CustomerName, TaxId, ContactName, Phone, DeliveryAddress, TaxType
            FROM dbo.Customer
            WHERE IsActive = 1
            ORDER BY CustomerCode, CustomerName;

            SELECT ProductID, ProductCode, ProductName, Specification, Unit, UnitPrice, Thickness, Width, Length
            FROM dbo.Product
            WHERE IsActive = 1
            ORDER BY ProductCode, ProductName;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            customers.Add(new SalesOrderCustomerOption(
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
            products.Add(new SalesOrderProductOption(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetDecimal(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8)));
        }

        model.Customers = customers;
        model.Products = products;
    }

    public async Task<long> SaveAsync(SalesOrderEditViewModel model)
    {
        if (model.TaxType == SalesOrderTaxTypes.None) model.TaxRate = 0m;
        if (model.TaxType == SalesOrderTaxTypes.Exclusive) model.TaxRate = 5m;
        ValidateRequest(model);

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            if (model.TaxType == SalesOrderTaxTypes.Inclusive)
            {
                await using var legacyTax = connection.CreateCommand();
                legacyTax.Transaction = transaction;
                legacyTax.CommandText = "SELECT TaxRate FROM dbo.SalesOrder WITH (UPDLOCK, HOLDLOCK) WHERE SalesOrderID = @Id AND TaxType = 'Inclusive';";
                legacyTax.Parameters.AddWithValue("@Id", model.SalesOrderId!.Value);
                var storedRate = await legacyTax.ExecuteScalarAsync();
                if (storedRate is null or DBNull) throw new InvalidOperationException("請選擇不計稅或稅外加。");
                model.TaxRate = Convert.ToDecimal(storedRate);
            }
            var customer = await GetCustomerAsync(connection, transaction, model.CustomerId!.Value)
                ?? throw new InvalidOperationException("找不到選取的客戶，或該客戶已停用。");

            var normalizedLines = new List<NormalizedSalesOrderLine>();
            foreach (var input in model.Lines)
            {
                normalizedLines.Add(await NormalizeLineAsync(connection, transaction, input));
            }

            var totals = CalculateTotals(normalizedLines, model.TaxType, model.TaxRate);
            var salesOrderId = model.SalesOrderId;

            if (salesOrderId.HasValue)
            {
                await using var update = connection.CreateCommand();
                update.Transaction = transaction;
                update.CommandText = """
                    UPDATE dbo.SalesOrder
                    SET CustomerID = @CustomerID,
                        CustomerCode = @CustomerCode,
                        CustomerName = @CustomerName,
                        CustomerTaxId = @CustomerTaxId,
                        CustomerContactName = @CustomerContactName,
                        CustomerPhone = @CustomerPhone,
                        CustomerAddress = @CustomerAddress,
                        OrderDate = @OrderDate,
                        RequiredDate = @RequiredDate,
                        TaxType = @TaxType,
                        TaxRate = @TaxRate,
                        SubTotal = @SubTotal,
                        TaxAmount = @TaxAmount,
                        TotalAmount = @TotalAmount,
                        IsCustomerSuppliedMaterial = @IsCustomerSuppliedMaterial,
                        Remark = @Remark,
                        PdfFileName = COALESCE(@PdfFileName, PdfFileName),
                        PdfStoragePath = COALESCE(@PdfStoragePath, PdfStoragePath),
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE SalesOrderID = @SalesOrderID
                      AND Status = 'Confirmed'
                      AND NOT EXISTS (SELECT 1 FROM dbo.SalesOrderItem WITH (UPDLOCK, HOLDLOCK) WHERE SalesOrderID = @SalesOrderID AND ShippedQty > 0);
                    """;
                AddHeaderParameters(update, model, customer, totals);
                update.Parameters.AddWithValue("@SalesOrderID", salesOrderId.Value);
                if (await update.ExecuteNonQueryAsync() == 0)
                {
                    throw new InvalidOperationException("只有「已確認」且尚未出貨的訂購單可以修改。");
                }

                await using var deleteItems = connection.CreateCommand();
                deleteItems.Transaction = transaction;
                deleteItems.CommandText = "DELETE FROM dbo.SalesOrderItem WHERE SalesOrderID = @SalesOrderID;";
                deleteItems.Parameters.AddWithValue("@SalesOrderID", salesOrderId.Value);
                await deleteItems.ExecuteNonQueryAsync();
            }
            else
            {
                var orderNo = await GetNextNumberAsync(connection, transaction, "SALES_ORDER");
                await using var insert = connection.CreateCommand();
                insert.Transaction = transaction;
                insert.CommandText = """
                    INSERT INTO dbo.SalesOrder (
                        SONo, CustomerID, CustomerCode, CustomerName, CustomerTaxId,
                        CustomerContactName, CustomerPhone, CustomerAddress,
                        OrderDate, RequiredDate, Status, TaxType, TaxRate,
                        SubTotal, TaxAmount, TotalAmount, IsCustomerSuppliedMaterial,
                        Remark, PdfFileName, PdfStoragePath, UpdatedAt
                    )
                    OUTPUT inserted.SalesOrderID
                    VALUES (
                        @OrderNo, @CustomerID, @CustomerCode, @CustomerName, @CustomerTaxId,
                        @CustomerContactName, @CustomerPhone, @CustomerAddress,
                        @OrderDate, @RequiredDate, 'Confirmed', @TaxType, @TaxRate,
                        @SubTotal, @TaxAmount, @TotalAmount, @IsCustomerSuppliedMaterial,
                        @Remark, @PdfFileName, @PdfStoragePath, SYSUTCDATETIME()
                    );
                    """;
                insert.Parameters.AddWithValue("@OrderNo", orderNo);
                AddHeaderParameters(insert, model, customer, totals);
                salesOrderId = Convert.ToInt64(await insert.ExecuteScalarAsync());
            }

            for (var index = 0; index < normalizedLines.Count; index++)
            {
                await InsertOrderLineAsync(connection, transaction, salesOrderId!.Value, index + 1, normalizedLines[index], model.TaxRate);
            }

            await transaction.CommitAsync();
            return salesOrderId!.Value;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task VoidAsync(long salesOrderId, string? reason)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.SalesOrder
            SET Status = 'Voided',
                VoidReason = @VoidReason,
                VoidedAt = SYSUTCDATETIME(),
                UpdatedAt = SYSUTCDATETIME()
            WHERE SalesOrderID = @SalesOrderID
              AND Status = 'Confirmed'
                      AND NOT EXISTS (SELECT 1 FROM dbo.SalesOrderItem WITH (UPDLOCK, HOLDLOCK) WHERE SalesOrderID = @SalesOrderID AND ShippedQty > 0);
            """;
        command.Parameters.AddWithValue("@SalesOrderID", salesOrderId);
        command.Parameters.AddWithValue("@VoidReason", DbValue(reason));

        if (await command.ExecuteNonQueryAsync() == 0)
        {
            throw new InvalidOperationException("只有「已確認」且尚未出貨的訂購單可以作廢。");
        }
    }

    private static void ValidateRequest(SalesOrderEditViewModel model)
    {
        if (!model.CustomerId.HasValue)
        {
            throw new InvalidOperationException("請選擇客戶。");
        }

        if (model.RequiredDate.Date < model.OrderDate.Date)
        {
            throw new InvalidOperationException("預交日期不可早於訂單日期。");
        }

        if (model.TaxType is not (SalesOrderTaxTypes.None or SalesOrderTaxTypes.Inclusive or SalesOrderTaxTypes.Exclusive) ||
            (model.TaxType == SalesOrderTaxTypes.Inclusive && !model.SalesOrderId.HasValue))
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
            SELECT CustomerCode, CustomerName, TaxId, ContactName, Phone, DeliveryAddress
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

    private static async Task<NormalizedSalesOrderLine> NormalizeLineAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        SalesOrderLineInputModel input)
    {
        if (input.Quantity <= 0)
        {
            throw new InvalidOperationException("商品數量必須大於 0。");
        }

        if (input.Quantity != decimal.Round(input.Quantity, 4))
        {
            throw new InvalidOperationException("商品數量最多只能有四位小數。");
        }

        if (input.UnitPrice != decimal.Round(input.UnitPrice, 4))
        {
            throw new InvalidOperationException("商品單價最多只能有四位小數。");
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
        return new NormalizedSalesOrderLine(
            input.ProductId,
            input.ProductId.HasValue ? "Product" : "Manual",
            FirstNonEmpty(input.ProductCode, defaultCode),
            productName,
            NullIfWhiteSpace(input.Thickness),
            NullIfWhiteSpace(input.Width),
            NullIfWhiteSpace(input.Length),
            FirstNonEmpty(input.Specification, defaultSpecification),
            FirstNonEmpty(input.Unit, defaultUnit),
            input.Quantity,
            input.UnitPrice,
            lineAmount,
            NullIfWhiteSpace(input.Remark));
    }

    private static SalesOrderTotals CalculateTotals(
        IReadOnlyList<NormalizedSalesOrderLine> lines,
        string taxType,
        decimal taxRate)
    {
        var lineTotal = RoundMoney(lines.Sum(line => line.LineAmount));
        if (taxType == SalesOrderTaxTypes.Inclusive)
        {
            var tax = taxRate == 0 ? 0 : RoundMoney(lineTotal * taxRate / (100m + taxRate));
            return new SalesOrderTotals(lineTotal - tax, tax, lineTotal);
        }

        var subtotal = Math.Round(lineTotal, 0, MidpointRounding.AwayFromZero);
        var exclusiveTax = Math.Round(subtotal * taxRate / 100m, 0, MidpointRounding.AwayFromZero);
        return new SalesOrderTotals(subtotal, exclusiveTax, subtotal + exclusiveTax);
    }

    private static void AddHeaderParameters(
        SqlCommand command,
        SalesOrderEditViewModel model,
        CustomerSnapshot customer,
        SalesOrderTotals totals)
    {
        command.Parameters.AddWithValue("@CustomerID", customer.CustomerId);
        command.Parameters.AddWithValue("@CustomerCode", customer.CustomerCode);
        command.Parameters.AddWithValue("@CustomerName", customer.CustomerName);
        command.Parameters.AddWithValue("@CustomerTaxId", DbValue(customer.TaxId));
        command.Parameters.AddWithValue("@CustomerContactName", DbValue(customer.ContactName));
        command.Parameters.AddWithValue("@CustomerPhone", DbValue(customer.Phone));
        command.Parameters.AddWithValue("@CustomerAddress", DbValue(customer.Address));
        command.Parameters.AddWithValue("@OrderDate", model.OrderDate.Date);
        command.Parameters.AddWithValue("@RequiredDate", model.RequiredDate.Date);
        command.Parameters.AddWithValue("@TaxType", model.TaxType);
        command.Parameters.AddWithValue("@TaxRate", model.TaxRate);
        command.Parameters.AddWithValue("@IsCustomerSuppliedMaterial", model.IsCustomerSuppliedMaterial);
        command.Parameters.AddWithValue("@SubTotal", totals.Subtotal);
        command.Parameters.AddWithValue("@TaxAmount", totals.TaxAmount);
        command.Parameters.AddWithValue("@TotalAmount", totals.TotalAmount);
        command.Parameters.AddWithValue("@Remark", DbValue(model.Remark));
        command.Parameters.AddWithValue("@PdfFileName", DbValue(model.PdfFileName));
        command.Parameters.AddWithValue("@PdfStoragePath", DbValue(model.PdfStoragePath));
    }

    private static async Task InsertOrderLineAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        long salesOrderId,
        int lineNumber,
        NormalizedSalesOrderLine line,
        decimal taxRate)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO dbo.SalesOrderItem (
                SalesOrderID, LineNumber, ProductID, ItemSource,
                ProductCode, ProductName, Specification, Unit, Thickness, Width, Length,
                OrderedQty, UnitPrice, TaxRate, Remark
            )
            VALUES (
                @SalesOrderID, @LineNumber, @ProductID, @ItemSource,
                @ProductCode, @ProductName, @Specification, @Unit, @Thickness, @Width, @Length,
                @OrderedQty, @UnitPrice, @TaxRate, @Remark
            );
            """;
        command.Parameters.AddWithValue("@SalesOrderID", salesOrderId);
        command.Parameters.AddWithValue("@LineNumber", lineNumber);
        command.Parameters.AddWithValue("@ProductID", DbValue(line.ProductId));
        command.Parameters.AddWithValue("@ItemSource", line.ItemSource);
        command.Parameters.AddWithValue("@ProductCode", DbValue(line.ProductCode));
        command.Parameters.AddWithValue("@ProductName", line.ProductName);
        command.Parameters.AddWithValue("@Thickness", DbValue(line.Thickness));
        command.Parameters.AddWithValue("@Width", DbValue(line.Width));
        command.Parameters.AddWithValue("@Length", DbValue(line.Length));
        command.Parameters.AddWithValue("@Specification", DbValue(line.Specification));
        command.Parameters.AddWithValue("@Unit", DbValue(line.Unit));
        command.Parameters.AddWithValue("@OrderedQty", line.Quantity);
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

    private sealed record NormalizedSalesOrderLine(
        long? ProductId,
        string ItemSource,
        string? ProductCode,
        string ProductName,
        string? Thickness,
        string? Width,
        string? Length,
        string? Specification,
        string? Unit,
        decimal Quantity,
        decimal UnitPrice,
        decimal LineAmount,
        string? Remark);

    private sealed record SalesOrderTotals(
        decimal Subtotal,
        decimal TaxAmount,
        decimal TotalAmount);

}
