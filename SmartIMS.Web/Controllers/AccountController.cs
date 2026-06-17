using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartIMS.Web.Services;
using SmartIMS.Web.ViewModels;

namespace SmartIMS.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly AuthService _authService;
    private readonly SystemSettingsService _settingsService;

    public AccountController(AuthService authService, SystemSettingsService settingsService)
    {
        _authService = authService;
        _settingsService = settingsService;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel
        {
            ReturnUrl = returnUrl,
            Branding = await _settingsService.GetBrandingAsync()
        });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        model.Branding = await _settingsService.GetBrandingAsync();
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _authService.ValidateUserAsync(model.UserName, model.Password);
        if (user is null)
        {
            ModelState.AddModelError("", "帳號或密碼不正確，或帳號已停用。");
            return View(model);
        }

        await _authService.SignInAsync(HttpContext, user);
        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
