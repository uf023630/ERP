using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartIMS.Web.Security;
using SmartIMS.Web.Services;
using SmartIMS.Web.ViewModels;

namespace SmartIMS.Web.Controllers;

[Authorize]
[RequirePermission("PAGE_SALES_QUOTATION")]
public sealed class QuotationController : Controller
{
    private readonly QuotationService _quotationService;

    public QuotationController(QuotationService quotationService)
    {
        _quotationService = quotationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(long? id = null)
    {
        var model = await _quotationService.GetListAsync();
        var selectedId = id.HasValue && model.Quotations.Any(item => item.SalesQuoteId == id.Value)
            ? id.Value
            : model.Quotations.FirstOrDefault()?.SalesQuoteId;

        if (selectedId.HasValue)
        {
            model.SelectedQuotationId = selectedId.Value;
            model.SelectedQuotation = await _quotationService.GetDetailAsync(selectedId.Value);
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Detail(long id)
    {
        var model = await _quotationService.GetDetailAsync(id);
        return model is null
            ? NotFound()
            : PartialView("_Detail", model);
    }

    [HttpGet]
    [RequirePermission("PAGE_SALES_QUOTATION_CREATE")]
    public async Task<IActionResult> Create()
    {
        return View("Edit", await _quotationService.CreateEditModelAsync());
    }

    [HttpGet]
    [RequirePermission("PAGE_SALES_QUOTATION_COPY")]
    public async Task<IActionResult> Copy(long id)
    {
        var model = await _quotationService.CreateCopyModelAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = "已帶入原報價內容，儲存後會產生新的報價單號";
        return View("Edit", model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var model = await _quotationService.GetEditModelAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        await PopulateNavigationAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(QuotationEditViewModel model)
    {
        var requiredPermission = model.SalesQuoteId.HasValue
            ? "PAGE_SALES_QUOTATION_EDIT"
            : "PAGE_SALES_QUOTATION_CREATE";
        if (!User.HasClaim(AuthService.PermissionClaimType, requiredPermission))
        {
            return Forbid();
        }

        RemoveUnusedBlankLines(model);
        if (model.Lines.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Lines), "請至少加入一項商品");
        }

        if (model.ExpireDate.Date < model.QuoteDate.Date)
        {
            ModelState.AddModelError(nameof(model.ExpireDate), "有效日期不可早於報價日期");
        }

        if (!ModelState.IsValid)
        {
            EnsureBlankLine(model);
            await _quotationService.PopulateLookupsAsync(model);
            await PopulateNavigationAsync(model);
            return View("Edit", model);
        }

        try
        {
            var id = await _quotationService.SaveAsync(model);
            TempData["StatusMessage"] = "報價單已儲存";
            return RedirectToAction(nameof(Edit), new { id });
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError("", exception.Message);
            EnsureBlankLine(model);
            await _quotationService.PopulateLookupsAsync(model);
            await PopulateNavigationAsync(model);
            return View("Edit", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("PAGE_SALES_QUOTATION_DELETE")]
    public async Task<IActionResult> Void(long id, string? reason)
    {
        try
        {
            await _quotationService.VoidAsync(id, reason);
            TempData["StatusMessage"] = "報價單已作廢";
        }
        catch (InvalidOperationException exception)
        {
            TempData["StatusMessage"] = exception.Message;
        }

        return RedirectToAction(nameof(Index), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("PAGE_SALES_QUOTATION_EDIT")]
    public async Task<IActionResult> Convert(long id)
    {
        try
        {
            var result = await _quotationService.ConvertToSalesOrderAsync(id);
            TempData["StatusMessage"] = $"已轉成銷售單 {result.SalesOrderNo}";
            return RedirectToAction(nameof(Index), new { id });
        }
        catch (InvalidOperationException exception)
        {
            TempData["StatusMessage"] = exception.Message;
            return RedirectToAction(nameof(Index), new { id });
        }
    }

    private async Task PopulateNavigationAsync(QuotationEditViewModel model)
    {
        if (!model.SalesQuoteId.HasValue)
        {
            return;
        }

        var quotations = (await _quotationService.GetListAsync()).Quotations;
        var index = quotations
            .Select((quotation, position) => new { quotation.SalesQuoteId, Position = position })
            .FirstOrDefault(item => item.SalesQuoteId == model.SalesQuoteId.Value)
            ?.Position ?? -1;

        if (index < 0)
        {
            return;
        }

        ViewData["FirstQuotationId"] = quotations[0].SalesQuoteId;
        ViewData["PreviousQuotationId"] = quotations[Math.Max(0, index - 1)].SalesQuoteId;
        ViewData["NextQuotationId"] = quotations[Math.Min(quotations.Count - 1, index + 1)].SalesQuoteId;
        ViewData["LastQuotationId"] = quotations[^1].SalesQuoteId;
    }

    private void RemoveUnusedBlankLines(QuotationEditViewModel model)
    {
        for (var index = model.Lines.Count - 1; index >= 0; index--)
        {
            var line = model.Lines[index];
            var isBlank = !line.ProductId.HasValue &&
                string.IsNullOrWhiteSpace(line.ProductCode) &&
                string.IsNullOrWhiteSpace(line.ProductName) &&
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

    private static void EnsureBlankLine(QuotationEditViewModel model)
    {
        if (model.Lines.Count == 0)
        {
            model.Lines.Add(new QuotationLineInputModel());
        }
    }
}
