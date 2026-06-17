using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SmartIMS.Web.Infrastructure;
using SmartIMS.Web.Models;
using SmartIMS.Web.Security;

namespace SmartIMS.Web.Services;

public sealed class AuthService
{
    public const string PermissionClaimType = "smartims.permission";
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly PermissionService _permissionService;

    public AuthService(SqlConnectionFactory connectionFactory, PermissionService permissionService)
    {
        _connectionFactory = connectionFactory;
        _permissionService = permissionService;
    }

    public async Task<AuthUser?> ValidateUserAsync(string userName, string password)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (1) AppUserID, UserName, DisplayName, Department, Email, PasswordHash, IsActive
            FROM dbo.AppUser
            WHERE UserName = @UserName;
            """;
        command.Parameters.AddWithValue("@UserName", userName.Trim());

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        var appUserId = reader.GetInt64(0);
        var passwordHash = reader.IsDBNull(5) ? null : reader.GetString(5);
        var isActive = reader.GetBoolean(6);
        if (!isActive || string.IsNullOrWhiteSpace(passwordHash) || !PasswordHasher.VerifyPassword(password, passwordHash))
        {
            return null;
        }

        var user = new AuthUser(
            appUserId,
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            passwordHash,
            isActive,
            await _permissionService.GetUserPermissionCodesAsync(appUserId));

        await TouchLastLoginAsync(appUserId);
        return user;
    }

    public async Task SignInAsync(HttpContext httpContext, AuthUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.AppUserId.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new("display_name", user.DisplayName)
        };

        claims.AddRange(user.Permissions.Select(permission => new Claim(PermissionClaimType, permission)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });
    }

    private async Task TouchLastLoginAsync(long appUserId)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE dbo.AppUser SET LastLoginAt = SYSUTCDATETIME(), UpdatedAt = SYSUTCDATETIME() WHERE AppUserID = @AppUserID;";
        command.Parameters.AddWithValue("@AppUserID", appUserId);
        await command.ExecuteNonQueryAsync();
    }
}
