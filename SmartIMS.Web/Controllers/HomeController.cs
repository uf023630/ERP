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
    private readonly CustomerService _customerService;
    private readonly ProductService _productService;

    public HomeController(ListViewSettingsService listViewSettingsService, ProductMaterialService productMaterialService, CustomerService customerService, ProductService productService)
    {
        _listViewSettingsService = listViewSettingsService;
        _productMaterialService = productMaterialService;
        _customerService = customerService;
        _productService = productService;
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
    public async Task<IActionResult> CustomerLayoutTest()
    {
        return View(await _customerService.GetAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("PAGE_CUSTOMER_LAYOUT_TEST")]
    public async Task<IActionResult> SaveCustomer([FromBody] CustomerItem request)
    {
        var permission = request.CustomerId.HasValue ? "PAGE_CUSTOMER_LAYOUT_TEST_EDIT" : "PAGE_CUSTOMER_LAYOUT_TEST_CREATE";
        if (!User.HasClaim(AuthService.PermissionClaimType, permission)) return Forbid();
        if (!ModelState.IsValid) return BadRequest(new { message = "請確認必填欄位、Email 格式與欄位長度。" });
        try { return Ok(await _customerService.SaveAsync(request)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("PAGE_CUSTOMER_LAYOUT_TEST")]
    [RequirePermission("PAGE_CUSTOMER_LAYOUT_TEST_DELETE")]
    public async Task<IActionResult> DeleteCustomer([FromBody] CustomerItem request)
    {
        if (!request.CustomerId.HasValue) return BadRequest(new { message = "請選擇客戶。" });
        try { await _customerService.DeleteAsync(request.CustomerId.Value); return Ok(new { deleted = true }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [RequirePermission("PAGE_PRODUCT_LAYOUT_TEST")]
    public async Task<IActionResult> ProductLayoutTest()
    {
        return View(new ProductPageViewModel
        {
            Products = await _productService.GetAsync(),
            Materials = await _productMaterialService.GetMaterialsAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("PAGE_PRODUCT_LAYOUT_TEST")]
    public async Task<IActionResult> SaveProduct([FromBody] ProductItem request)
    {
        var permission = request.ProductId.HasValue ? "PAGE_PRODUCT_LAYOUT_TEST_EDIT" : "PAGE_PRODUCT_LAYOUT_TEST_CREATE";
        if (!User.HasClaim(AuthService.PermissionClaimType, permission)) return Forbid();
        if (!ModelState.IsValid) return BadRequest(new { message = "請確認商品必填欄位、單價與欄位長度。" });
        try { return Ok(await _productService.SaveAsync(request)); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("PAGE_PRODUCT_LAYOUT_TEST")]
    [RequirePermission("PAGE_PRODUCT_LAYOUT_TEST_DELETE")]
    public async Task<IActionResult> DeleteProduct([FromBody] ProductDeleteRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(new { message = "請選擇商品。" });
        try { await _productService.DeleteAsync(request.ProductId); return Ok(new { deleted = true }); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
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
    [RequirePermission("PAGE_PRODUCT_MATERIAL")]
    public async Task<IActionResult> SaveMaterial([FromBody] ProductMaterialSaveRequest request)
    {
        var permission = request.ProductMaterialId.HasValue ? "PAGE_PRODUCT_MATERIAL_EDIT" : "PAGE_PRODUCT_MATERIAL_CREATE";
        if (!User.HasClaim(AuthService.PermissionClaimType, permission)) return Forbid();
        if (!ModelState.IsValid) return BadRequest(new { message = "請確認材質編碼、名稱與欄位長度。" });
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
    [RequirePermission("PAGE_PRODUCT_MATERIAL")]
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
