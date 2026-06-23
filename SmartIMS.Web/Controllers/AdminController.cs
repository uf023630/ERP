using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartIMS.Web.Models;
using SmartIMS.Web.Security;
using SmartIMS.Web.Services;
using SmartIMS.Web.ViewModels;
using System.Security.Claims;

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
    public async Task<IActionResult> CreateUser()
    {
        return View("EditUser", await CreateUserEditModelAsync(new AdminUserEditViewModel()));
    }

    [RequirePermission("ADMIN_USERS_MANAGE")]
    [HttpGet]
    public async Task<IActionResult> EditUser(long id)
    {
        var user = await _adminService.GetUserForEditAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        return View(await CreateUserEditModelAsync(user));
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

        if (model.AppUserId.HasValue && IsCurrentUser(model.AppUserId.Value))
        {
            var currentRoleIds = await _permissionService.GetUserRoleIdsAsync(model.AppUserId.Value);
            if (!currentRoleIds.SetEquals(model.SelectedRoleIds))
            {
                ModelState.AddModelError(nameof(model.SelectedRoleIds), "不可變更目前登入帳號的所屬組別，避免失去管理權限");
            }
        }

        if (!ModelState.IsValid)
        {
            model = await CreateUserEditModelAsync(model);
            return View("EditUser", model);
        }

        var appUserId = await _adminService.SaveUserAsync(model);
        await _permissionService.ReplaceUserRolesAsync(appUserId, model.SelectedRoleIds);
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

    [RequirePermission("ADMIN_USERS_MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateUser(long id)
    {
        var currentUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (long.TryParse(currentUserIdValue, out var currentUserId) && currentUserId == id)
        {
            return BadRequest(new { message = "不可停用目前登入中的帳號" });
        }

        var updated = await _adminService.DeactivateUserAsync(id);
        if (!updated)
        {
            return NotFound(new { message = "找不到要停用的使用者" });
        }

        return Ok(new { message = "使用者已停用" });
    }

    [RequirePermission("PAGE_ADMIN_PERMISSIONS")]
    [HttpGet]
    public async Task<IActionResult> Permissions(long? roleId)
    {
        return View(await _permissionService.GetRolePermissionEditModelAsync(roleId));
    }

    [RequirePermission("ADMIN_PERMISSIONS_MANAGE")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePermissions(long roleId, long[] pageActionIds)
    {
        await _permissionService.ReplaceRolePermissionsAsync(roleId, pageActionIds);
        TempData["StatusMessage"] = "組別權限已儲存，使用者重新登入後生效";
        return RedirectToAction(nameof(Permissions), new { roleId });
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

    private async Task<AdminUserEditViewModel> CreateUserEditModelAsync(AdminUserEditViewModel model)
    {
        model.AvailableRoles = await _permissionService.GetRolesAsync();
        if (model.AppUserId.HasValue && model.SelectedRoleIds.Count == 0)
        {
            model.SelectedRoleIds = await _permissionService.GetUserRoleIdsAsync(model.AppUserId.Value);
        }

        return model;
    }

    private bool IsCurrentUser(long appUserId)
    {
        var currentUserIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(currentUserIdValue, out var currentUserId) && currentUserId == appUserId;
    }
}
