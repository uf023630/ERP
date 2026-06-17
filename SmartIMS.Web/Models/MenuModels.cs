namespace SmartIMS.Web.Models;

public sealed record MenuItemDefinition(string Text, string Controller, string Action, string PermissionCode);

public sealed record MenuGroupDefinition(string Text, IReadOnlyList<MenuItemDefinition> Items);
