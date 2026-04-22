using Microsoft.AspNetCore.Mvc;
using ProzorroAnalytics.Application.Interfaces.Services;

namespace ProzorroAnalytics.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProzorroController : ControllerBase
{
    private readonly IImportJobQueue _importJobQueue;

    public ProzorroController(IImportJobQueue importJobQueue)
    {
        _importJobQueue = importJobQueue;
    }

    [HttpPost("import-tenders")]
    public IActionResult ImportTenders()
    {
        var enqueued = _importJobQueue.TryEnqueue();

        if (!enqueued)
            return Conflict(new { message = "An import job is already queued or running." });

        return Accepted(new { message = "Import job enqueued." });
    }
}
