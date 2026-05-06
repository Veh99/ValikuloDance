using System.ComponentModel.DataAnnotations;

namespace ValikuloDance.Application.DTOs.Auth
{
    public class ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        public required string Email { get; set; }
    }
}
