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
    private readonly ProductMaterialService _productMaterialService;

    public HomeController(ListViewSettingsService listViewSettingsService, ProductMaterialService productMaterialService)
    {
        _listViewSettingsService = listViewSettingsService;
        _productMaterialService = productMaterialService;
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

    [RequirePermission("PAGE_CUSTOMER_LAYOUT_TEST")]
    public IActionResult CustomerLayoutTest()
    {
        return View();
    }

    [RequirePermission("PAGE_PRODUCT_LAYOUT_TEST")]
    public IActionResult ProductLayoutTest()
    {
        return View();
    }

    [RequirePermission("PAGE_PRODUCT_MATERIAL")]
    public async Task<IActionResult> MaterialLayoutTest()
    {
        var model = MaterialLayoutTestViewModel.Create(await _productMaterialService.GetMaterialsAsync());
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (long.TryParse(userIdValue, out var appUserId))
        {
            await _listViewSettingsService.ApplySettingsAsync(appUserId, model.ListView);
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireAnyPermission("PAGE_PRODUCT_MATERIAL_CREATE", "PAGE_PRODUCT_MATERIAL_EDIT")]
    public async Task<IActionResult> SaveMaterial([FromBody] ProductMaterialSaveRequest request)
    {
        try
        {
            var material = await _productMaterialService.SaveMaterialAsync(request);
            return Ok(material);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("PAGE_PRODUCT_MATERIAL_DELETE")]
    public async Task<IActionResult> DeleteMaterial([FromBody] ProductMaterialDeleteRequest request)
    {
        try
        {
            await _productMaterialService.DeleteMaterialAsync(request.ProductMaterialId);
            return Ok(new { deleted = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
