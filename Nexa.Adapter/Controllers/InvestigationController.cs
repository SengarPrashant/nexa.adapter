using Microsoft.AspNetCore.Mvc;
using Nexa.Adapter.Models;
using Nexa.Adapter.Services;

namespace Nexa.Adapter.Controllers
{
    [Route("api/v1/nexa/[controller]")]
    [ApiController]
    public class InvestigationController(IInvestigationOrchestrator investigationOrchestrator) : ControllerBase
    {
        private readonly IInvestigationOrchestrator _investigationOrchestrator= investigationOrchestrator;

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze(Alert alert)
        {
            var result = await _investigationOrchestrator.AnalyzeAsync(new Alert() { Id = 1, CustomerId = 5513 });
            return Ok(result);
        }
    }
}
