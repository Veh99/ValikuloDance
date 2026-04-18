using System.ComponentModel.DataAnnotations;

namespace ValikuloDance.Application.DTOs.Auth
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Имя обязательно")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Имя должно содержать от 2 до 100 символов")]
        public required string Name { get; set; }

        [EmailAddress(ErrorMessage = "Неверный формат email")]
        public required string? Email { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен содержать минимум 6 символов")]
        public required string Password { get; set; }

        public required string TelegramUsername { get; set; }
    }
}
