using System.ComponentModel.DataAnnotations;

namespace ValikuloDance.Application.DTOs.Auth
{
    public class UpdateEmailDto
    {
        [EmailAddress(ErrorMessage = "Неверный формат email")]
        [StringLength(100, ErrorMessage = "Email не должен превышать 100 символов")]
        public string? Email { get; set; }
    }
}
