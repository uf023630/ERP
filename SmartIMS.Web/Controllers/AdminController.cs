using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartIMS.Web.Models;
using SmartIMS.Web.Security;
using SmartIMS.Web.Services;
using SmartIMS.Web.ViewModels;

namespace SmartIMS.Web.Controllers;

[Authorize]
public sealed class AdminController : Controller
{
    private readonly AdminService _adminService;
    private readonly PermissionService _permissionService;
    private readonly SystemSettingsService _settingsService;
    private readonly IWebHostEnvironment _environment;

    public AdminController(AdminService adminService, PermissionService permissionService, SystemSettingsService settingsService, IWebHostEnvironment environment)
    {
        _adminService = adminService;
        _permissionService = permissionService;
        _settingsService = settingsService;
        _environment = environment;
    }

    [RequirePermission("PAGE_ADMIN_USERS")]
    public async Task<IActionResult> Users()
    {
        return View(new AdminUserListViewModel { Users = await _adminService.GetUsersAsync() });
    }

    [RequirePermission("ADMIN_USERS_MANAGE")]
    [HttpGet]
    public IActionResult CreateUser()
    {
        return View("EditUser", new AdminUserEditViewModel());
    }

    [RequirePermission("ADMIN_USERS_MANAGE")]
    [HttpGet]
    public async Task<IActionResult> EditUser(long id)
    {
        var user = await _adminService.GetUserForEditAsync(id);
        return user is null ? NotFound() : View(user);
    }

    [RequirePermission("ADMIN_USERS_MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveUser(AdminUserEditViewModel model)
    {
        if (model.AppUserId is null && string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "新增使用者時必須輸入密碼");
        }

        if (!ModelState.IsValid)
        {
            return View("EditUser", model);
        }

        await _adminService.SaveUserAsync(model);
        TempData["StatusMessage"] = "使用者資料已儲存";
        return RedirectToAction(nameof(Users));
    }

    [RequirePermission("ADMIN_USERS_MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(long id, string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            TempData["StatusMessage"] = "密碼長度至少需要 8 個字元";
            return RedirectToAction(nameof(Users));
        }

        await _adminService.ResetPasswordAsync(id, password);
        TempData["StatusMessage"] = "密碼已重設";
        return RedirectToAction(nameof(Users));
    }

    [RequirePermission("PAGE_ADMIN_PERMISSIONS")]
    [HttpGet]
    public async Task<IActionResult> Permissions(long? userId)
    {
        var users = await _adminService.GetUsersAsync();
        var selectedUser = userId.HasValue ? users.FirstOrDefault(user => user.AppUserId == userId.Value) : users.FirstOrDefault();

        if (selectedUser is null)
        {
            return View(new UserPermissionEditViewModel { Permissions = await _permissionService.GetAllPermissionsAsync() });
        }

        return View(new UserPermissionEditViewModel
        {
            AppUserId = selectedUser.AppUserId,
            UserName = selectedUser.UserName,
            DisplayName = selectedUser.DisplayName,
            Permissions = await _permissionService.GetAllPermissionsAsync(),
            SelectedPermissionIds = await _permissionService.GetUserPermissionIdsAsync(selectedUser.AppUserId)
        });
    }

    [RequirePermission("ADMIN_PERMISSIONS_MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePermissions(long appUserId, long[] permissionIds)
    {
        await _permissionService.ReplaceUserPermissionsAsync(appUserId, permissionIds);
        TempData["StatusMessage"] = "使用者權限已儲存，重新登入後生效";
        return RedirectToAction(nameof(Permissions), new { userId = appUserId });
    }

    [RequirePermission("PAGE_ADMIN_BRANDING")]
    [HttpGet]
    public async Task<IActionResult> Branding()
    {
        var branding = await _settingsService.GetBrandingAsync();
        var skin = await _settingsService.GetSkinAsync();
        return View(new BrandingViewModel
        {
            CompanyName = branding.CompanyName,
            CompanyLogoPath = branding.CompanyLogoPath,
            LoginHeroImagePath = branding.LoginHeroImagePath,
            LoginTitle = branding.LoginTitle,
            LoginSubtitle = branding.LoginSubtitle,
            SkinKey = skin.SkinKey
        });
    }

    [RequirePermission("ADMIN_BRANDING_MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Branding(BrandingViewModel model, IFormFile? companyLogo, IFormFile? loginHeroImage)
    {
        if (!ModelState.IsValid)
        {
            model.AvailableSkins = SystemSkin.AvailableSkins;
            return View(model);
        }

        if (!SystemSkin.IsValidSkinKey(model.SkinKey))
        {
            ModelState.AddModelError(nameof(model.SkinKey), "請選擇有效的系統皮膚");
            model.AvailableSkins = SystemSkin.AvailableSkins;
            return View(model);
        }

        var logoPath = await SaveBrandingFileAsync(companyLogo) ?? model.CompanyLogoPath;
        var heroPath = await SaveBrandingFileAsync(loginHeroImage) ?? model.LoginHeroImagePath;

        await _settingsService.SaveBrandingAsync(
            new SystemBranding(
                model.CompanyName.Trim(),
                logoPath,
                heroPath,
                model.LoginTitle.Trim(),
                model.LoginSubtitle.Trim()),
            SystemSkin.Create(model.SkinKey));

        TempData["StatusMessage"] = "公司形象設定已儲存";
        return RedirectToAction(nameof(Branding));
    }

    private async Task<string?> SaveBrandingFileAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".png", ".jpg", ".jpeg", ".gif", ".webp" };
        if (!allowed.Contains(extension))
        {
            throw new InvalidOperationException("Only image files are allowed.");
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = $"/uploads/branding/{fileName}";
        var absolutePath = Path.Combine(_environment.WebRootPath, "uploads", "branding", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using var stream = System.IO.File.Create(absolutePath);
        await file.CopyToAsync(stream);
        return relativePath;
    }
}
