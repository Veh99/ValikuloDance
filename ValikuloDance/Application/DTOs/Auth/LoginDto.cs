using System.ComponentModel.DataAnnotations;

namespace ValikuloDance.Application.DTOs.Auth
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Телефон обязателен")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        public required string Password { get; set; }
    }
}
