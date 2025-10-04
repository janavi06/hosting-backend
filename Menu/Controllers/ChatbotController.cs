using Microsoft.AspNetCore.Mvc;
using Restaurant_Menu.Services;
using System.Threading.Tasks;

namespace Restaurant_Menu.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly ChatbotService _chatbotService;

        public ChatbotController(ChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] string question, [FromQuery] int restaurantId)
        {
            if (string.IsNullOrWhiteSpace(question))
                return BadRequest("Question is required.");

            var answer = await _chatbotService.AskAsync(question, restaurantId);
            return Ok(new { answer });
        }
    }
}
