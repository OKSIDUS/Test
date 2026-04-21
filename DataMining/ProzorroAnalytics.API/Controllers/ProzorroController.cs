using Microsoft.AspNetCore.Mvc;
using ProzorroAnalytics.Application.Interfaces.Services;

namespace ProzorroAnalytics.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProzorroController : Controller
    {
        private readonly IImportService importService;
        public ProzorroController(IImportService importService)
        {
            this.importService = importService;
        }

        [HttpPost("import-tenders")]
        public async Task<IActionResult> ImportTenders(CancellationToken ct)
        {
            await importService.ImportTendersAsync(ct);
            return Ok();

        }
    }
}
