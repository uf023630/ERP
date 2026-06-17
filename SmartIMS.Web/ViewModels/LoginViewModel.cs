using System.ComponentModel.DataAnnotations;
using SmartIMS.Web.Models;

namespace SmartIMS.Web.ViewModels;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "請輸入帳號")]
    [Display(Name = "帳號")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "請輸入密碼")]
    [DataType(DataType.Password)]
    [Display(Name = "密碼")]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }

    public SystemBranding Branding { get; set; } = new("智慧進銷存系統", null, null, "智慧進銷存系統", "請使用管理員提供的帳號登入");
}
