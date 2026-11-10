using System.ComponentModel.DataAnnotations;
using Microsoft.Data.SqlClient;
using SmartIMS.Web.Infrastructure;
using SmartIMS.Web.ViewModels;

namespace SmartIMS.Web.Services;

public sealed class ProductMaterialService
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ProductMaterialService(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ProductMaterialItem>> GetMaterialsAsync()
    {
        var materials = new List<ProductMaterialItem>();
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ProductMaterialID, MaterialCode, MaterialName
            FROM dbo.ProductMaterial
            WHERE IsActive = 1
            ORDER BY MaterialCode;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            materials.Add(new ProductMaterialItem(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2)));
        }

        return materials;
    }

    public async Task<ProductMaterialItem> SaveMaterialAsync(ProductMaterialSaveRequest request)
    {
        var validationErrors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, new ValidationContext(request), validationErrors, true) || request.ProductMaterialId is <= 0)
            throw new InvalidOperationException("請確認材質編碼、名稱與欄位長度。");
        var materialCode = (request.MaterialCode ?? "").Trim();
        var materialName = (request.MaterialName ?? "").Trim();
        if (string.IsNullOrWhiteSpace(materialCode) || string.IsNullOrWhiteSpace(materialName))
        {
            throw new InvalidOperationException("材質編碼與材質名稱不可空白。");
        }

        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await EnsureMaterialCodeAvailableAsync(connection, request.ProductMaterialId, materialCode);

        if (request.ProductMaterialId is null)
        {
            await using var insertCommand = connection.CreateCommand();
            insertCommand.CommandText = """
                INSERT INTO dbo.ProductMaterial (MaterialCode, MaterialName)
                OUTPUT inserted.ProductMaterialID
                VALUES (@MaterialCode, @MaterialName);
                """;
            insertCommand.Parameters.AddWithValue("@MaterialCode", materialCode);
            insertCommand.Parameters.AddWithValue("@MaterialName", materialName);
            long newId;
            try { newId = Convert.ToInt64(await insertCommand.ExecuteScalarAsync()); }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            { throw new InvalidOperationException("材質編碼已存在。", ex); }
            return new ProductMaterialItem(newId, materialCode, materialName);
        }

        await using var updateCommand = connection.CreateCommand();
        updateCommand.CommandText = """
            UPDATE dbo.ProductMaterial
            SET MaterialCode = @MaterialCode,
                MaterialName = @MaterialName,
                IsActive = 1,
                UpdatedAt = SYSUTCDATETIME()
            WHERE ProductMaterialID = @ProductMaterialID AND IsActive = 1;
            """;
        updateCommand.Parameters.AddWithValue("@ProductMaterialID", request.ProductMaterialId.Value);
        updateCommand.Parameters.AddWithValue("@MaterialCode", materialCode);
        updateCommand.Parameters.AddWithValue("@MaterialName", materialName);

        int affected;
        try { affected = await updateCommand.ExecuteNonQueryAsync(); }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        { throw new InvalidOperationException("材質編碼已存在。", ex); }
        if (affected == 0)
        {
            throw new InvalidOperationException("找不到要修改的商品材質。");
        }

        return new ProductMaterialItem(request.ProductMaterialId.Value, materialCode, materialName);
    }

    public async Task DeleteMaterialAsync(long productMaterialId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
        await using var check = connection.CreateCommand();
        check.Transaction = transaction;
        check.CommandText = "SELECT ProductMaterialID FROM dbo.ProductMaterial WITH (UPDLOCK, HOLDLOCK) WHERE ProductMaterialID = @Id AND IsActive = 1;";
        check.Parameters.AddWithValue("@Id", productMaterialId);
        if (await check.ExecuteScalarAsync() is null)
            throw new InvalidOperationException("找不到要刪除的商品材質。");

        await using var usage = connection.CreateCommand();
        usage.Transaction = transaction;
        usage.CommandText = "SELECT COUNT(1) FROM dbo.Product WHERE ProductMaterialID = @Id AND IsActive = 1;";
        usage.Parameters.AddWithValue("@Id", productMaterialId);
        if (Convert.ToInt32(await usage.ExecuteScalarAsync()) > 0)
            throw new InvalidOperationException("此材質仍有商品使用，請先修改或停用相關商品。");

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE dbo.ProductMaterial SET IsActive = 0, UpdatedAt = SYSUTCDATETIME() WHERE ProductMaterialID = @Id;";
        command.Parameters.AddWithValue("@Id", productMaterialId);
        await command.ExecuteNonQueryAsync();
        await transaction.CommitAsync();
    }

    private static async Task EnsureMaterialCodeAvailableAsync(SqlConnection connection, long? productMaterialId, string materialCode)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(1)
            FROM dbo.ProductMaterial
            WHERE MaterialCode = @MaterialCode
              AND IsActive = 1
              AND (@ProductMaterialID IS NULL OR ProductMaterialID <> @ProductMaterialID);
            """;
        command.Parameters.AddWithValue("@MaterialCode", materialCode);
        command.Parameters.AddWithValue("@ProductMaterialID", (object?)productMaterialId ?? DBNull.Value);

        if (Convert.ToInt32(await command.ExecuteScalarAsync()) > 0)
        {
            throw new InvalidOperationException("材質編碼已存在。");
        }
    }
}
