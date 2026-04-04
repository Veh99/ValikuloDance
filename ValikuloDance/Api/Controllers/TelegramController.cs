using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Telegram.Bot.Types;
using ValikuloDance.Application.DTOs.Telegram;
using ValikuloDance.Application.Interfaces;

namespace ValikuloDance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TelegramController : ControllerBase
    {
        private readonly ITelegramService _telegramService;

        public TelegramController(ITelegramService telegramService)
        {
            _telegramService = telegramService;
        }

        [Authorize]
        [HttpPost("chat-binding")]
        public async Task<IActionResult> UpsertChatBinding([FromBody] UpsertTelegramChatBindingRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirstValue("userId")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Пользователь не авторизован" });

            await _telegramService.UpsertChatBindingAsync(userId, request.TelegramChatId, request.TelegramUsername);

            return Ok(new { message = "Привязка Telegram чата сохранена" });
        }

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] Update update)
        {
            await _telegramService.HandleUpdateAsync(update);
            return Ok();
        }
    }
}
