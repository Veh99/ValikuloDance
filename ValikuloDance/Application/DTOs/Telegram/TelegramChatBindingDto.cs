using System.ComponentModel.DataAnnotations;

namespace ValikuloDance.Application.DTOs.Telegram
{
    public class UpsertTelegramChatBindingRequest
    {
        [Required(ErrorMessage = "TelegramChatId обязателен")]
        [StringLength(100, ErrorMessage = "TelegramChatId не должен превышать 100 символов")]
        public required string TelegramChatId { get; set; }

        [StringLength(50, ErrorMessage = "TelegramUsername не должен превышать 50 символов")]
        public string? TelegramUsername { get; set; }
    }
}
