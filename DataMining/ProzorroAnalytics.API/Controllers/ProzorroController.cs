using Microsoft.AspNetCore.Mvc;
using ProzorroAnalytics.Application.Interfaces.Services;

namespace ProzorroAnalytics.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProzorroController : ControllerBase
{
    private readonly IImportJobQueue _importJobQueue;
    private readonly IAnalyticsService _analyticsService;

    public ProzorroController(IImportJobQueue importJobQueue, IAnalyticsService analyticsService)
    {
        _importJobQueue = importJobQueue;
        _analyticsService = analyticsService;
    }

    [HttpPost("import-tenders")]
    public IActionResult ImportTenders()
    {
        var enqueued = _importJobQueue.TryEnqueue();

        if (!enqueued)
            return Conflict(new { message = "An import job is already queued or running." });

        return Accepted(new { message = "Import job enqueued." });
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var dashboard = await _analyticsService.GetDashboardAsync(ct);
        return Ok(dashboard);
    }
}
