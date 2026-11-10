using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.Data.SqlClient;
using SmartIMS.Web.Infrastructure;
using SmartIMS.Web.ViewModels;

namespace SmartIMS.Web.Services;

public sealed class ProductService(SqlConnectionFactory factory)
{
    public async Task<IReadOnlyList<ProductItem>> GetAsync()
    {
        var result = new List<ProductItem>();
        await using var connection = factory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.ProductID, p.ProductCode, p.ProductName, p.ProductMaterialID,
                   m.MaterialName, p.Thickness, p.Length, p.Width,
                   p.Specification, p.Unit, p.UnitPrice, p.Note
            FROM dbo.Product p
            LEFT JOIN dbo.ProductMaterial m ON m.ProductMaterialID = p.ProductMaterialID
            WHERE p.IsActive = 1
            ORDER BY p.ProductCode;
            """;
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            string Text(int index) => reader.IsDBNull(index) ? "" : reader.GetString(index);
            result.Add(new ProductItem
            {
                ProductId = reader.GetInt64(0), ProductCode = Text(1), ProductName = Text(2),
                ProductMaterialId = reader.IsDBNull(3) ? null : reader.GetInt64(3),
                Material = Text(4), Thickness = Text(5), Length = Text(6), Width = Text(7),
                Spec = Text(8), Unit = Text(9), Price = reader.GetDecimal(10), Note = Text(11)
            });
        }
        return result;
    }

    public async Task<ProductItem> SaveAsync(ProductItem item)
    {
        item.ProductCode = (item.ProductCode ?? "").Trim();
        item.ProductName = (item.ProductName ?? "").Trim();
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(item, new ValidationContext(item), errors, true)
            || item.ProductId is <= 0 || item.ProductMaterialId is <= 0)
            throw new InvalidOperationException("請確認商品編號、名稱、材質、單價與欄位長度。");
        if (decimal.Round(item.Price, 2) != item.Price)
            throw new InvalidOperationException("單價最多只能有兩位小數。");

        await using var connection = factory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        item.Material = "";
        if (item.ProductMaterialId.HasValue)
        {
            await using var material = connection.CreateCommand();
            material.Transaction = transaction;
            // Hold the material lock until saving completes so it cannot be deactivated concurrently.
            material.CommandText = "SELECT MaterialName FROM dbo.ProductMaterial WITH (HOLDLOCK) WHERE ProductMaterialID = @Id AND IsActive = 1;";
            material.Parameters.AddWithValue("@Id", item.ProductMaterialId.Value);
            var name = await material.ExecuteScalarAsync();
            if (name is not string materialName)
                throw new InvalidOperationException("所選材質不存在或已停用，請重新選擇。");
            item.Material = materialName;
        }

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = item.ProductId.HasValue ? """
            UPDATE dbo.Product
            SET ProductCode = @Code, ProductName = @Name, ProductMaterialID = @MaterialId,
                Thickness = @Thickness, Length = @Length, Width = @Width,
                Specification = @Spec, Unit = @Unit, UnitPrice = @Price, Note = @Note,
                UpdatedAt = SYSUTCDATETIME()
            OUTPUT inserted.ProductID
            WHERE ProductID = @Id AND IsActive = 1;
            """ : """
            INSERT INTO dbo.Product
                (ProductCode, ProductName, ProductMaterialID, Thickness, Length, Width,
                 Specification, Unit, UnitPrice, Note)
            OUTPUT inserted.ProductID
            VALUES (@Code, @Name, @MaterialId, @Thickness, @Length, @Width, @Spec, @Unit, @Price, @Note);
            """;
        command.Parameters.AddWithValue("@Id", (object?)item.ProductId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Code", item.ProductCode);
        command.Parameters.AddWithValue("@Name", item.ProductName);
        command.Parameters.AddWithValue("@MaterialId", (object?)item.ProductMaterialId ?? DBNull.Value);
        command.Parameters.AddWithValue("@Thickness", (item.Thickness ?? "").Trim());
        command.Parameters.AddWithValue("@Length", (item.Length ?? "").Trim());
        command.Parameters.AddWithValue("@Width", (item.Width ?? "").Trim());
        command.Parameters.AddWithValue("@Spec", (item.Spec ?? "").Trim());
        command.Parameters.AddWithValue("@Unit", (item.Unit ?? "").Trim());
        var price = command.Parameters.Add("@Price", SqlDbType.Decimal);
        price.Precision = 18;
        price.Scale = 2;
        price.Value = item.Price;
        command.Parameters.AddWithValue("@Note", (item.Note ?? "").Trim());
        try
        {
            var id = await command.ExecuteScalarAsync();
            if (id is null) throw new InvalidOperationException("找不到商品，可能已被刪除。");
            item.ProductId = Convert.ToInt64(id);
            await transaction.CommitAsync();
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            throw new InvalidOperationException("商品編號已存在，請使用其他編號。", ex);
        }
        return item;
    }

    public async Task DeleteAsync(long productId)
    {
        await using var connection = factory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE dbo.Product SET IsActive = 0, UpdatedAt = SYSUTCDATETIME() WHERE ProductID = @Id AND IsActive = 1;";
        command.Parameters.AddWithValue("@Id", productId);
        if (await command.ExecuteNonQueryAsync() == 0)
            throw new InvalidOperationException("找不到商品，可能已被刪除。");
    }
}
