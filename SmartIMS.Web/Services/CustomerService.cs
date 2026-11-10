using System.Globalization;
using Microsoft.Data.SqlClient;
using SmartIMS.Web.Infrastructure;
using SmartIMS.Web.ViewModels;
namespace SmartIMS.Web.Services;
public sealed class CustomerService(SqlConnectionFactory factory)
{
    public async Task<IReadOnlyList<CustomerItem>> GetAsync()
    {
        var result = new List<CustomerItem>();
        await using var connection = factory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CustomerID, CustomerName, ContactName, Phone, Fax, Email, LineId, TaxId, ZipCode, Address, PaymentMethod, BankAccount, Note, CustomerCode FROM dbo.Customer WHERE IsActive = 1 ORDER BY CustomerID;";
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            result.Add(new CustomerItem { CustomerId = reader.GetInt64(0),
                CustomerName = reader.IsDBNull(1) ? "" : reader.GetString(1),
                Contact = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Phone = reader.IsDBNull(3) ? "" : reader.GetString(3),
                Fax = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Email = reader.IsDBNull(5) ? "" : reader.GetString(5),
                Line = reader.IsDBNull(6) ? "" : reader.GetString(6),
                TaxId = reader.IsDBNull(7) ? "" : reader.GetString(7),
                ZipCode = reader.IsDBNull(8) ? "" : reader.GetString(8),
                DeliveryAddress = reader.IsDBNull(9) ? "" : reader.GetString(9),
                PaymentMethod = reader.IsDBNull(10) ? "" : reader.GetString(10),
                BankAccount = reader.IsDBNull(11) ? "" : reader.GetString(11),
                Note = reader.IsDBNull(12) ? "" : reader.GetString(12),
                CustomerCode = reader.GetString(13) });
        return result;
    }
    public async Task<CustomerItem> SaveAsync(CustomerItem item)
    {
        if (string.IsNullOrWhiteSpace(item.CustomerName)) throw new InvalidOperationException("公司名稱不可空白。");
        if (item.CustomerId is <= 0) throw new InvalidOperationException("客戶編號無效。");
        await using var connection = factory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        string? newCode = null;
        if (!item.CustomerId.HasValue)
        {
            await using var sequence = connection.CreateCommand();
            sequence.Transaction = transaction;
            sequence.CommandText = """
                UPDATE dbo.NumberSequence WITH (UPDLOCK, HOLDLOCK)
                SET CurrentValue = CurrentValue + 1, UpdatedAt = SYSUTCDATETIME()
                OUTPUT inserted.CurrentValue
                WHERE SequenceKey = N'CUSTOMER' AND Prefix = N'C' AND PaddingLength = 5
                  AND CurrentValue >= 0 AND CurrentValue < 99999;
                """;
            var number = await sequence.ExecuteScalarAsync();
            if (number is null) throw new InvalidOperationException("客戶流水號設定不存在或已達 C99999，請聯絡管理員。");
            newCode = "C" + Convert.ToInt64(number).ToString("D5", CultureInfo.InvariantCulture);
        }
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = item.CustomerId.HasValue
            ? "UPDATE dbo.Customer SET CustomerName = @CustomerName, ContactName = @Contact, Phone = @Phone, Fax = @Fax, Email = @Email, LineId = @Line, TaxId = @TaxId, ZipCode = @ZipCode, Address = @DeliveryAddress, PaymentMethod = @PaymentMethod, BankAccount = @BankAccount, Note = @Note OUTPUT inserted.CustomerID, inserted.CustomerCode WHERE CustomerID = @Id AND IsActive = 1;"
            : "INSERT INTO dbo.Customer (CustomerCode, CustomerName, ContactName, Phone, Fax, Email, LineId, TaxId, ZipCode, Address, PaymentMethod, BankAccount, Note) OUTPUT inserted.CustomerID, inserted.CustomerCode VALUES (@Code, @CustomerName, @Contact, @Phone, @Fax, @Email, @Line, @TaxId, @ZipCode, @DeliveryAddress, @PaymentMethod, @BankAccount, @Note);";
        command.Parameters.AddWithValue("@Id", (object?)item.CustomerId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Code", (object?)newCode ?? DBNull.Value);
        item.CustomerName = (item.CustomerName ?? "").Trim();
        command.Parameters.AddWithValue("@CustomerName", item.CustomerName);
        item.Contact = (item.Contact ?? "").Trim();
        command.Parameters.AddWithValue("@Contact", item.Contact);
        item.Phone = (item.Phone ?? "").Trim();
        command.Parameters.AddWithValue("@Phone", item.Phone);
        item.Fax = (item.Fax ?? "").Trim();
        command.Parameters.AddWithValue("@Fax", item.Fax);
        item.Email = (item.Email ?? "").Trim();
        command.Parameters.AddWithValue("@Email", (object?)item.Email ?? DBNull.Value);
        item.Line = (item.Line ?? "").Trim();
        command.Parameters.AddWithValue("@Line", item.Line);
        item.TaxId = (item.TaxId ?? "").Trim();
        command.Parameters.AddWithValue("@TaxId", item.TaxId);
        item.ZipCode = (item.ZipCode ?? "").Trim();
        command.Parameters.AddWithValue("@ZipCode", item.ZipCode);
        item.DeliveryAddress = (item.DeliveryAddress ?? "").Trim();
        command.Parameters.AddWithValue("@DeliveryAddress", item.DeliveryAddress);
        item.PaymentMethod = (item.PaymentMethod ?? "").Trim();
        command.Parameters.AddWithValue("@PaymentMethod", item.PaymentMethod);
        item.BankAccount = (item.BankAccount ?? "").Trim();
        command.Parameters.AddWithValue("@BankAccount", item.BankAccount);
        item.Note = (item.Note ?? "").Trim();
        command.Parameters.AddWithValue("@Note", item.Note);
        await using (var reader = await command.ExecuteReaderAsync())
        {
            if (!await reader.ReadAsync()) throw new InvalidOperationException("找不到客戶，可能已被刪除。");
            item.CustomerId = reader.GetInt64(0);
            item.CustomerCode = reader.GetString(1);
        }
        await transaction.CommitAsync();
        return item;
    }
    public async Task DeleteAsync(long id)
    {
        await using var connection = factory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE dbo.Customer SET IsActive = 0 WHERE CustomerID = @Id AND IsActive = 1;";
        command.Parameters.AddWithValue("@Id", id);
        if (await command.ExecuteNonQueryAsync() == 0) throw new InvalidOperationException("找不到客戶，可能已被刪除。");
    }
}
