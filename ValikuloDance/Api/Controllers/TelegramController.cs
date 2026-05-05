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
        private readonly IConfiguration _configuration;

        public TelegramController(ITelegramService telegramService, IConfiguration configuration)
        {
            _telegramService = telegramService;
            _configuration = configuration;
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
            var configuredSecret = _configuration["Telegram:WebhookSecretToken"];
            if (string.IsNullOrWhiteSpace(configuredSecret))
                return NotFound();

            if (!Request.Headers.TryGetValue("X-Telegram-Bot-Api-Secret-Token", out var providedSecret) ||
                providedSecret != configuredSecret)
            {
                return Unauthorized();
            }

            await _telegramService.HandleUpdateAsync(update);
            return Ok();
        }
    }
}
