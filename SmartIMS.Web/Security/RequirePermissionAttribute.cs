using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SmartIMS.Web.Services;

namespace SmartIMS.Web.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string permissionCode) : base(typeof(RequirePermissionFilter))
    {
        Arguments = [permissionCode];
    }
}

public sealed class RequirePermissionFilter : IAuthorizationFilter
{
    private readonly string _permissionCode;

    public RequirePermissionFilter(string permissionCode)
    {
        _permissionCode = permissionCode;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        if (!context.HttpContext.User.HasClaim(AuthService.PermissionClaimType, _permissionCode))
        {
            context.Result = new ForbidResult();
        }
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireAnyPermissionAttribute : TypeFilterAttribute
{
    public RequireAnyPermissionAttribute(params string[] permissionCodes) : base(typeof(RequireAnyPermissionFilter))
    {
        Arguments = [permissionCodes];
    }
}

public sealed class RequireAnyPermissionFilter : IAuthorizationFilter
{
    private readonly IReadOnlyList<string> _permissionCodes;

    public RequireAnyPermissionFilter(string[] permissionCodes)
    {
        _permissionCodes = permissionCodes;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        if (!_permissionCodes.Any(code => context.HttpContext.User.HasClaim(AuthService.PermissionClaimType, code)))
        {
            context.Result = new ForbidResult();
        }
    }
}
