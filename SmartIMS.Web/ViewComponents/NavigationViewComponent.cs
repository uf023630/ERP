using Microsoft.AspNetCore.Mvc;
using SmartIMS.Web.Services;

namespace SmartIMS.Web.ViewComponents;

public sealed class NavigationViewComponent : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        var permissions = UserClaimsPrincipal.Claims
            .Where(claim => claim.Type == AuthService.PermissionClaimType)
            .Select(claim => claim.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var groups = PermissionService.MenuGroups
            .Select(group => group with
            {
                Items = group.Items.Where(item => permissions.Contains(item.PermissionCode)).ToList()
            })
            .Where(group => group.Items.Count > 0)
            .ToList();

        return View(groups);
    }
}
