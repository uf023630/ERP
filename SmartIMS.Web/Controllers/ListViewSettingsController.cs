using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartIMS.Web.Models;
using SmartIMS.Web.Services;

namespace SmartIMS.Web.Controllers;

[Authorize]
[Route("[controller]")]
public sealed class ListViewSettingsController : Controller
{
    private readonly ListViewSettingsService _settingsService;

    public ListViewSettingsController(ListViewSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([FromBody] ListViewSettingsSaveRequest request)
    {
        var appUserId = GetCurrentAppUserId();
        var allowedColumnKeys = ListViewRegistry.GetAllowedColumnKeys(request.ListKey);
        if (appUserId is null || allowedColumnKeys.Count == 0)
        {
            return BadRequest();
        }

        await _settingsService.SaveSettingsAsync(appUserId.Value, request.ListKey, allowedColumnKeys, request.Columns);
        return Ok(new { saved = true });
    }

    [HttpPost("Reset")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset([FromBody] ListViewResetRequest request)
    {
        var appUserId = GetCurrentAppUserId();
        if (appUserId is null || ListViewRegistry.GetAllowedColumnKeys(request.ListKey).Count == 0)
        {
            return BadRequest();
        }

        await _settingsService.ResetSettingsAsync(appUserId.Value, request.ListKey);
        return Ok(new { reset = true });
    }

    private long? GetCurrentAppUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(value, out var appUserId) ? appUserId : null;
    }

    public sealed class ListViewResetRequest
    {
        public string ListKey { get; init; } = "";
    }
}
