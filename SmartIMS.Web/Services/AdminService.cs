using Microsoft.Data.SqlClient;
using SmartIMS.Web.Infrastructure;
using SmartIMS.Web.Security;
using SmartIMS.Web.ViewModels;

namespace SmartIMS.Web.Services;

public sealed class AdminService
{
    private readonly SqlConnectionFactory _connectionFactory;

    public AdminService(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<AdminUserRow>> GetUsersAsync()
    {
        var users = new List<AdminUserRow>();
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT u.AppUserID, u.UserName, u.DisplayName, u.Department, u.Email, u.IsActive, u.LastLoginAt, COALESCE(roles.RoleNames, N'') AS RoleNames
            FROM dbo.AppUser u
            OUTER APPLY (
              SELECT STRING_AGG(CAST(r.RoleName AS NVARCHAR(MAX)), N'、') AS RoleNames
              FROM dbo.UserRole ur
              INNER JOIN dbo.AppRole r ON r.RoleID = ur.RoleID AND r.IsActive = 1
              WHERE ur.AppUserID = u.AppUserID
            ) roles
            ORDER BY u.AppUserID;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            users.Add(new AdminUserRow(
                reader.GetInt64(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.GetBoolean(5),
                reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                reader.IsDBNull(7) ? "" : reader.GetString(7)));
        }

        return users;
    }

    public async Task<AdminUserEditViewModel?> GetUserForEditAsync(long appUserId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT AppUserID, UserName, DisplayName, Department, Email, IsActive FROM dbo.AppUser WHERE AppUserID = @AppUserID;";
        command.Parameters.AddWithValue("@AppUserID", appUserId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new AdminUserEditViewModel
        {
            AppUserId = reader.GetInt64(0),
            UserName = reader.GetString(1),
            DisplayName = reader.GetString(2),
            Department = reader.IsDBNull(3) ? null : reader.GetString(3),
            Email = reader.IsDBNull(4) ? null : reader.GetString(4),
            IsActive = reader.GetBoolean(5)
        };
    }

    public async Task<long> SaveUserAsync(AdminUserEditViewModel model)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        if (model.AppUserId is null)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO dbo.AppUser (UserName, DisplayName, Department, Email, PasswordHash, IsActive)
                OUTPUT inserted.AppUserID
                VALUES (@UserName, @DisplayName, @Department, @Email, @PasswordHash, @IsActive);
                """;
            command.Parameters.AddWithValue("@UserName", model.UserName.Trim());
            command.Parameters.AddWithValue("@DisplayName", model.DisplayName.Trim());
            command.Parameters.AddWithValue("@Department", (object?)model.Department ?? DBNull.Value);
            command.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("@PasswordHash", PasswordHasher.HashPassword(model.Password ?? "Admin@12345"));
            command.Parameters.AddWithValue("@IsActive", model.IsActive);
            return Convert.ToInt64(await command.ExecuteScalarAsync());
        }

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                UPDATE dbo.AppUser
                SET DisplayName = @DisplayName, Department = @Department, Email = @Email, IsActive = @IsActive, UpdatedAt = SYSUTCDATETIME()
                WHERE AppUserID = @AppUserID;
                """;
            command.Parameters.AddWithValue("@AppUserID", model.AppUserId.Value);
            command.Parameters.AddWithValue("@DisplayName", model.DisplayName.Trim());
            command.Parameters.AddWithValue("@Department", (object?)model.Department ?? DBNull.Value);
            command.Parameters.AddWithValue("@Email", (object?)model.Email ?? DBNull.Value);
            command.Parameters.AddWithValue("@IsActive", model.IsActive);
            await command.ExecuteNonQueryAsync();
        }

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            await ResetPasswordAsync(model.AppUserId.Value, model.Password);
        }

        return model.AppUserId.Value;
    }

    public async Task ResetPasswordAsync(long appUserId, string password)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE dbo.AppUser SET PasswordHash = @PasswordHash, UpdatedAt = SYSUTCDATETIME() WHERE AppUserID = @AppUserID;";
        command.Parameters.AddWithValue("@AppUserID", appUserId);
        command.Parameters.AddWithValue("@PasswordHash", PasswordHasher.HashPassword(password));
        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> DeactivateUserAsync(long appUserId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE dbo.AppUser
            SET IsActive = 0, UpdatedAt = SYSUTCDATETIME()
            WHERE AppUserID = @AppUserID;
            """;
        command.Parameters.AddWithValue("@AppUserID", appUserId);
        return await command.ExecuteNonQueryAsync() > 0;
    }
}
