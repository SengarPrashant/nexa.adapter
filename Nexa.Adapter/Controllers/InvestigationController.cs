using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nexa.Adapter.Services;

namespace Nexa.Adapter.Controllers
{
    [Route("api/v1/nexa/[controller]")]
    [ApiController]
    public class InvestigationController(IInvestigationOrchestrator investigationOrchestrator) : ControllerBase
    {
        private readonly IInvestigationOrchestrator _investigationOrchestrator= investigationOrchestrator;

        [HttpPost("/analyze")]
        public async Task<IActionResult> Analyze()
        {
            return Ok();
        }
    }
}
