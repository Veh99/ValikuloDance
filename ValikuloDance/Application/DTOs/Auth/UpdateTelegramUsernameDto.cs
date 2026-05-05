using System.ComponentModel.DataAnnotations;

namespace ValikuloDance.Application.DTOs.Auth
{
    public class UpdateTelegramUsernameDto
    {
        [Required(ErrorMessage = "Telegram username обязателен")]
        [RegularExpression(@"^@?[A-Za-z][A-Za-z0-9_]{4,31}$",
            ErrorMessage = "Введите Telegram username: 5-32 символа, латиница, цифры и _, можно с @")]
        public required string TelegramUsername { get; set; }
    }
}
