using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nexa.Adapter.Models;
using Nexa.Adapter.Services;

namespace Nexa.Adapter.Controllers
{
    [Route("api/nexa/[controller]")]
    [ApiController]
    public class ChatController(IChatService chatService) : ControllerBase
    {
        private readonly IChatService _chatService= chatService;

        [HttpPost("complete")]
        public async Task<IActionResult> Complete(ChatRequest request)
        {
            var result = await _chatService.ProcessChat(request);
            return Ok(result);
        }
    }
}
