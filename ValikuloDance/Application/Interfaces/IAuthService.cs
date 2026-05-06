using ValikuloDance.Application.DTOs.Auth;
using ValikuloDance.Domain.Entities;

namespace ValikuloDance.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);
        Task ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto);
        Task ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto);
        Task<UserDto> UpdateEmailAsync(Guid userId, UpdateEmailDto updateEmailDto);
        Task<UserDto> UpdateTelegramUsernameAsync(Guid userId, UpdateTelegramUsernameDto updateTelegramUsernameDto);
        Task LogoutAsync(Guid userId);
        Task<User?> GetUserByPhone(string phoneNumber);
    }
}
