namespace SmartIMS.Web.Models;

public sealed record AuthUser(
    long AppUserId,
    string UserName,
    string DisplayName,
    string? Department,
    string? Email,
    string? PasswordHash,
    bool IsActive,
    IReadOnlyList<string> Permissions);
