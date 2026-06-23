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
        var materialCode = request.MaterialCode.Trim();
        var materialName = request.MaterialName.Trim();
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
            var newId = Convert.ToInt64(await insertCommand.ExecuteScalarAsync());
            return new ProductMaterialItem(newId, materialCode, materialName);
        }

        await using var updateCommand = connection.CreateCommand();
        updateCommand.CommandText = """
            UPDATE dbo.ProductMaterial
            SET MaterialCode = @MaterialCode,
                MaterialName = @MaterialName,
                IsActive = 1,
                UpdatedAt = SYSUTCDATETIME()
            WHERE ProductMaterialID = @ProductMaterialID;
            """;
        updateCommand.Parameters.AddWithValue("@ProductMaterialID", request.ProductMaterialId.Value);
        updateCommand.Parameters.AddWithValue("@MaterialCode", materialCode);
        updateCommand.Parameters.AddWithValue("@MaterialName", materialName);

        if (await updateCommand.ExecuteNonQueryAsync() == 0)
        {
            throw new InvalidOperationException("找不到要修改的商品材質。");
        }

        return new ProductMaterialItem(request.ProductMaterialId.Value, materialCode, materialName);
    }

    public async Task DeleteMaterialAsync(long productMaterialId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.ProductMaterial
            SET IsActive = 0,
                UpdatedAt = SYSUTCDATETIME()
            WHERE ProductMaterialID = @ProductMaterialID;
            """;
        command.Parameters.AddWithValue("@ProductMaterialID", productMaterialId);

        if (await command.ExecuteNonQueryAsync() == 0)
        {
            throw new InvalidOperationException("找不到要刪除的商品材質。");
        }
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
