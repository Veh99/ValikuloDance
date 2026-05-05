using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ValikuloDance.Application.DTOs.Auth;
using ValikuloDance.Application.Interfaces;

namespace ValikuloDance.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Регистрация нового пользователя
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _authService.RegisterAsync(registerDto);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при регистрации");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Вход в систему
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var result = await _authService.LoginAsync(loginDto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при входе");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Обновление токена
        /// </summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(refreshTokenDto);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Смена пароля
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = Guid.Parse(User.FindFirstValue("userId"));
                var result = await _authService.ChangePasswordAsync(userId, changePasswordDto);
                return Ok(new { message = "Пароль успешно изменен" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Смена Telegram username текущего пользователя
        /// </summary>
        [HttpPatch("telegram-username")]
        [Authorize]
        public async Task<IActionResult> UpdateTelegramUsername([FromBody] UpdateTelegramUsernameDto updateTelegramUsernameDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userIdClaim = User.FindFirstValue("userId")
                    ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("sub");

                if (string.IsNullOrWhiteSpace(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    return Unauthorized(new { message = "Пользователь не авторизован" });

                var user = await _authService.UpdateTelegramUsernameAsync(userId, updateTelegramUsernameDto);
                return Ok(new
                {
                    message = "Telegram username обновлен. Если вы меняли аккаунт Telegram, запустите бота и нажмите Start заново.",
                    user
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при смене Telegram username");
                return StatusCode(500, new { message = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("get-user-by-phone")]
        public async Task<IActionResult> GetUserId(string phoneNumber)
        {
            var user = await _authService.GetUserByPhone(phoneNumber);
            if (user is null)
                return BadRequest("Пользователь не найден");

            return Ok(new { user.Id });
        }
    }
}
