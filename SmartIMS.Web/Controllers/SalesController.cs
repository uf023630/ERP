using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartIMS.Web.Security;

namespace SmartIMS.Web.Controllers;

[Authorize]
public class SalesController : Controller
{
    [RequirePermission("PAGE_SALES_DASHBOARD")]
    public IActionResult Index()
    {
        return View();
    }
}
