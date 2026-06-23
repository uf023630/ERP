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
    string RoleNames);

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

    [Display(Name = "所屬組別")]
    public HashSet<long> SelectedRoleIds { get; set; } = [];

    public IReadOnlyList<RoleOptionViewModel> AvailableRoles { get; set; } = [];
}

public sealed record RoleOptionViewModel(
    long RoleId,
    string RoleCode,
    string RoleName,
    string? Description,
    bool IsSystemRole,
    bool IsActive);

public sealed class RolePermissionEditViewModel
{
    public long? SelectedRoleId { get; set; }
    public string SelectedRoleName { get; set; } = "";
    public IReadOnlyList<RoleOptionViewModel> Roles { get; set; } = [];
    public IReadOnlyList<RolePermissionPageViewModel> Pages { get; set; } = [];
    public HashSet<long> SelectedPageActionIds { get; set; } = [];
}

public sealed record RolePermissionPageViewModel(
    long PageId,
    string PageCode,
    string PageName,
    string MenuGroup,
    IReadOnlyList<RolePermissionActionViewModel> Actions);

public sealed record RolePermissionActionViewModel(
    long PageActionId,
    string ActionCode,
    string ActionName,
    string PermissionCode);

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
