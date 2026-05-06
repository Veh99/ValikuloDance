using System.ComponentModel.DataAnnotations;

namespace ValikuloDance.Application.DTOs.Auth
{
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Токен обязателен")]
        public required string Token { get; set; }

        [Required(ErrorMessage = "Новый пароль обязателен")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Пароль должен содержать минимум 6 символов")]
        public required string NewPassword { get; set; }
    }
}
