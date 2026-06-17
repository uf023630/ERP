namespace SmartIMS.Web.Models;

public sealed record PermissionDefinition(
    long PermissionId,
    string PermissionCode,
    string PermissionName,
    string ModuleCode,
    string ActionCode,
    string? Description,
    bool IsActive);
