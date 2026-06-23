using System.ComponentModel.DataAnnotations;
using SmartIMS.Web.Models;

namespace SmartIMS.Web.ViewModels;

public sealed class AdminUserListViewModel
{
    public IReadOnlyList<AdminUserRow> Users { get; set; } = [];
}

public sealed record AdminUserRow(
    long AppUserId,
    string UserName,
    string DisplayName,
    string? Department,
    string? Email,
    bool IsActive,
    DateTime? LastLoginAt,
    string PermissionNames);

public sealed class AdminUserEditViewModel
{
    public long? AppUserId { get; set; }

    [Required(ErrorMessage = "請輸入帳號")]
    [Display(Name = "帳號")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "請輸入顯示名稱")]
    [Display(Name = "顯示名稱")]
    public string DisplayName { get; set; } = "";

    [Display(Name = "部門")]
    public string? Department { get; set; }

    [EmailAddress(ErrorMessage = "Email 格式不正確")]
    [Display(Name = "Email")]
    public string? Email { get; set; }

    [Display(Name = "啟用")]
    public bool IsActive { get; set; } = true;

    [DataType(DataType.Password)]
    [Display(Name = "密碼")]
    public string? Password { get; set; }
}

public sealed class UserPermissionEditViewModel
{
    public long AppUserId { get; set; }
    public string UserName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public HashSet<long> SelectedPermissionIds { get; set; } = [];
    public IReadOnlyList<PermissionDefinition> Permissions { get; set; } = [];
}

public sealed class BrandingViewModel
{
    [Required(ErrorMessage = "請輸入公司名稱")]
    [Display(Name = "公司名稱")]
    public string CompanyName { get; set; } = "";

    [Required(ErrorMessage = "請輸入登入標題")]
    [Display(Name = "登入標題")]
    public string LoginTitle { get; set; } = "";

    [Required(ErrorMessage = "請輸入登入副標題")]
    [Display(Name = "登入副標題")]
    public string LoginSubtitle { get; set; } = "";

    [Required(ErrorMessage = "請選擇系統皮膚")]
    [Display(Name = "系統皮膚")]
    public string SkinKey { get; set; } = SystemSkin.DefaultSkinKey;

    public IReadOnlyList<SystemSkinOption> AvailableSkins { get; set; } = SystemSkin.AvailableSkins;

    public string? CompanyLogoPath { get; set; }
    public string? LoginHeroImagePath { get; set; }
}
