using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartIMS.Web.Models;
using SmartIMS.Web.Security;
using SmartIMS.Web.Services;
using SmartIMS.Web.ViewModels;

namespace SmartIMS.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ListViewSettingsService _listViewSettingsService;

    public HomeController(ListViewSettingsService listViewSettingsService)
    {
        _listViewSettingsService = listViewSettingsService;
    }

    [RequirePermission("PAGE_DASHBOARD")]
    public IActionResult Index()
    {
        return View();
    }

    [RequirePermission("PAGE_COMPONENT_TEST")]
    public async Task<IActionResult> ComponentTest()
    {
        var model = ComponentTestViewModel.Create();
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (long.TryParse(userIdValue, out var appUserId))
        {
            await _listViewSettingsService.ApplySettingsAsync(appUserId, model.ListView);
        }

        return View(model);
    }

    [RequirePermission("PAGE_COMPONENT_TEST")]
    public IActionResult CustomerLayoutTest()
    {
        return View();
    }

    [RequirePermission("PAGE_COMPONENT_TEST")]
    public IActionResult ProductLayoutTest()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
