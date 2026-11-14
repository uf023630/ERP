using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SmartIMS.Web.Security;
using SmartIMS.Web.Services;
using SmartIMS.Web.ViewModels;

namespace SmartIMS.Web.Controllers;

[Authorize]
[RequirePermission("PAGE_SALES_ORDER")]
public sealed class SalesOrderController : Controller
{
    private readonly SalesOrderService _salesOrderService;
    private readonly ILogger<SalesOrderController> _logger;
    private readonly OrderPdfStorage _pdfStorage;

    public SalesOrderController(SalesOrderService salesOrderService, ILogger<SalesOrderController> logger, OrderPdfStorage pdfStorage)
    {
        _salesOrderService = salesOrderService;
        _logger = logger;
        _pdfStorage = pdfStorage;
    }

    [HttpGet]
    public async Task<IActionResult> Index(long? id = null)
    {
        var model = await _salesOrderService.GetListAsync();
        var selectedId = id.HasValue && model.SalesOrders.Any(item => item.SalesOrderId == id.Value)
            ? id.Value
            : model.SalesOrders.FirstOrDefault()?.SalesOrderId;

        if (selectedId.HasValue)
        {
            model.SelectedSalesOrderId = selectedId.Value;
            model.SelectedSalesOrder = await _salesOrderService.GetDetailAsync(selectedId.Value);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(long id)
    {
        var model = await _salesOrderService.GetDetailAsync(id);
        return model is null
            ? NotFound()
            : PartialView("_Detail", model);
    }

    [HttpGet]
    [RequirePermission("PAGE_SALES_ORDER_CREATE")]
    public async Task<IActionResult> Create()
    {
        return View("Edit", await _salesOrderService.CreateEditModelAsync());
    }

    [HttpGet]
    [RequirePermission("PAGE_SALES_ORDER_COPY")]
    public async Task<IActionResult> Copy(long id)
    {
        var model = await _salesOrderService.CreateCopyModelAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "已帶入原訂單內容，儲存後會產生新的訂購單號";
        return View("Edit", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var model = await _salesOrderService.GetEditModelAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        await PopulateNavigationAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(22 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 22 * 1024 * 1024)]
    public async Task<IActionResult> Save(SalesOrderEditViewModel model, IFormFile? orderPdf)
    {
        var requiredPermission = model.SalesOrderId.HasValue
            ? "PAGE_SALES_ORDER_EDIT"
            : "PAGE_SALES_ORDER_CREATE";
        if (!User.HasClaim(AuthService.PermissionClaimType, requiredPermission))
        {
            return Forbid();
        }

        if (model.SalesOrderId.HasValue)
        {
            var existing = await _salesOrderService.GetEditModelAsync(model.SalesOrderId.Value);
            if (existing is null) return NotFound();
            if (!existing.IsEditable) return BadRequest("已作廢或已出貨的訂購單不可修改。");
            model.PdfFileName = existing.PdfFileName;
            model.SourceQuoteNo = existing.SourceQuoteNo;
        }
        RemoveUnusedBlankLines(model);
        if (model.Lines.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Lines), "請至少加入一項商品");
        }

        if (model.RequiredDate.Date < model.OrderDate.Date)
        {
            ModelState.AddModelError(nameof(model.RequiredDate), "預交日期不可早於訂單日期");
        }

        if (!ModelState.IsValid)
        {
            EnsureBlankLine(model);
            await _salesOrderService.PopulateLookupsAsync(model);
            await PopulateNavigationAsync(model);
            return View("Edit", model);
        }

        string? uploadedPath = null;
        var originalFileName = model.PdfFileName;
        try
        {
            if (orderPdf is not null)
            {
                var saved = await _pdfStorage.SaveAsync(orderPdf);
                model.PdfFileName = saved.FileName;
                model.PdfStoragePath = uploadedPath = saved.StoragePath;
            }
            var id = await _salesOrderService.SaveAsync(model);
            TempData["StatusMessage"] = "訂購單已儲存";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (InvalidOperationException exception)
        {
            _pdfStorage.TryDelete(uploadedPath);
            model.PdfFileName = originalFileName;
            model.PdfStoragePath = null;
            ModelState.AddModelError("", exception.Message);
            EnsureBlankLine(model);
            await _salesOrderService.PopulateLookupsAsync(model);
            await PopulateNavigationAsync(model);
            return View("Edit", model);
        }
        catch (SqlException exception)
        {
            _pdfStorage.TryDelete(uploadedPath);
            model.PdfFileName = originalFileName;
            model.PdfStoragePath = null;
            _logger.LogError(exception, "Failed to save sales order {SalesOrderId}", model.SalesOrderId);
            ModelState.AddModelError("", "資料庫儲存失敗，請稍後重試；若問題持續，請聯絡系統管理員查看日誌。");
            EnsureBlankLine(model);
            try
            {
                await _salesOrderService.PopulateLookupsAsync(model);
                await PopulateNavigationAsync(model);
            }
            catch (SqlException lookupException)
            {
                _logger.LogError(lookupException, "Failed to reload sales order lookups after a save error");
            }
            return View("Edit", model);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _pdfStorage.TryDelete(uploadedPath);
            model.PdfFileName = originalFileName;
            model.PdfStoragePath = null;
            _logger.LogError(exception, "Failed to store sales order PDF");
            ModelState.AddModelError("", "PDF 儲存失敗，請確認儲存目錄的寫入權限與可用空間。請重新選取檔案後重試。");
            await _salesOrderService.PopulateLookupsAsync(model);
            await PopulateNavigationAsync(model);
            return View("Edit", model);
        }

    }

    [HttpGet]
    public async Task<IActionResult> ViewPdf(long id)
    {
        var model = await _salesOrderService.GetDetailAsync(id);
        if (model is null || string.IsNullOrWhiteSpace(model.PdfStoragePath) || !System.IO.File.Exists(model.PdfStoragePath))
            return NotFound("找不到存查附件。");
        var disposition = new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue("inline");
        disposition.SetHttpFileName(model.PdfFileName ?? "attachment.pdf");
        Response.Headers.ContentDisposition = disposition.ToString();
        Response.Headers.CacheControl = "private, no-store";
        return PhysicalFile(model.PdfStoragePath, "application/pdf", enableRangeProcessing: true);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("PAGE_SALES_ORDER_DELETE")]
    public async Task<IActionResult> Void(long id, string? reason)
    {
        try
        {
            await _salesOrderService.VoidAsync(id, reason);
            TempData["StatusMessage"] = "訂購單已作廢";
        }
        catch (InvalidOperationException exception)
        {
            TempData["StatusMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index), new { id });
    }

    private async Task PopulateNavigationAsync(SalesOrderEditViewModel model)
    {
        if (!model.SalesOrderId.HasValue)
        {
            return;
        }

        var quotations = (await _salesOrderService.GetListAsync()).SalesOrders;
        var index = quotations
            .Select((quotation, position) => new { quotation.SalesOrderId, Position = position })
            .FirstOrDefault(item => item.SalesOrderId == model.SalesOrderId.Value)
            ?.Position ?? -1;

        if (index < 0)
        {
            return;
        }

        ViewData["FirstSalesOrderId"] = quotations[0].SalesOrderId;
        ViewData["PreviousSalesOrderId"] = quotations[Math.Max(0, index - 1)].SalesOrderId;
        ViewData["NextSalesOrderId"] = quotations[Math.Min(quotations.Count - 1, index + 1)].SalesOrderId;
        ViewData["LastSalesOrderId"] = quotations[^1].SalesOrderId;
    }

    private void RemoveUnusedBlankLines(SalesOrderEditViewModel model)
    {
        for (var index = model.Lines.Count - 1; index >= 0; index--)
        {
            var line = model.Lines[index];
            var isBlank = !line.ProductId.HasValue &&
                string.IsNullOrWhiteSpace(line.ProductCode) &&
                string.IsNullOrWhiteSpace(line.ProductName) &&
                string.IsNullOrWhiteSpace(line.Thickness) &&
                string.IsNullOrWhiteSpace(line.Width) &&
                string.IsNullOrWhiteSpace(line.Length) &&
                string.IsNullOrWhiteSpace(line.Specification) &&
                string.IsNullOrWhiteSpace(line.Unit) &&
                line.UnitPrice == 0;
            if (!isBlank)
            {
                continue;
            }

            model.Lines.RemoveAt(index);
            foreach (var key in ModelState.Keys
                         .Where(key => key.StartsWith($"Lines[{index}].", StringComparison.OrdinalIgnoreCase))
                         .ToList())
            {
                ModelState.Remove(key);
            }
        }
    }

    private static void EnsureBlankLine(SalesOrderEditViewModel model)
    {
        if (model.Lines.Count == 0)
        {
            model.Lines.Add(new SalesOrderLineInputModel());
        }
    }
}
