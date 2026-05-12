using Microsoft.EntityFrameworkCore;
using ValikuloDance.Domain.Entities;
using ValikuloDance.Infrastructure.Data;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using ValikuloDance.Application.Interfaces;
using ValikuloDance.Api.Settings;
using ValikuloDance.Application.DTOs.Auth;

namespace ValikuloDance.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly JwtSettings _jwtSettings;
        private readonly FrontendSettings _frontendSettings;

        public AuthService(
            AppDbContext context,
            ITokenService tokenService,
            IPasswordHasher passwordHasher,
            IEmailService emailService,
            IOptions<JwtSettings> jwtSettings,
            IOptions<FrontendSettings> frontendSettings)
        {
            _context = context;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _jwtSettings = jwtSettings.Value;
            _frontendSettings = frontendSettings.Value;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            if (!string.IsNullOrWhiteSpace(registerDto.Email))
            {
                var existingUserByEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == registerDto.Email);

                if (existingUserByEmail != null)
                    throw new InvalidOperationException("Пользователь с таким email уже существует");
            }

            var telegramUsername = registerDto.TelegramUsername.Trim().TrimStart('@');
            var passwordHash = _passwordHasher.HashPassword(registerDto.Password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = registerDto.Name,
                Phone = null,
                Email = string.IsNullOrWhiteSpace(registerDto.Email) ? null : registerDto.Email.Trim(),
                TelegramUsername = telegramUsername,
                TelegramChatId = null,
                Role = "Client",
                CreatedAt = DateTime.UtcNow,
                PasswordHash = passwordHash,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var trainer = await _context.Trainers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.IsActive);

            var token = _tokenService.GenerateToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                User = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Phone = user.Phone,
                    Email = user.Email,
                    Role = user.Role,
                    TelegramUsername = user.TelegramUsername,
                    IsTrainer = trainer != null,
                    TrainerId = trainer?.Id,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                }
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            // Ищем пользователя по email или phone
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == loginDto.Email || u.Phone == loginDto.Email);

            if (user == null)
                throw new UnauthorizedAccessException("Неверный email/телефон или пароль");

            // Проверяем пароль
            if (string.IsNullOrEmpty(user.PasswordHash) ||
                !_passwordHasher.VerifyPassword(user.PasswordHash, loginDto.Password))
                throw new UnauthorizedAccessException("Неверный email/телефон или пароль");

            // Обновляем время последнего входа
            user.LastLoginAt = DateTime.UtcNow;

            var trainer = await _context.Trainers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.IsActive);

            // Генерируем токены
            var token = _tokenService.GenerateToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Обновляем refresh token
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                User = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Phone = user.Phone,
                    Email = user.Email,
                    Role = user.Role,
                    TelegramUsername = user.TelegramUsername,
                    IsTrainer = trainer != null,
                    TrainerId = trainer?.Id,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                }
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshTokenDto.RefreshToken && !u.IsDeleted);

            if (user == null)
                throw new UnauthorizedAccessException("Неверный refresh token");

            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh token истек");

            var newToken = _tokenService.GenerateToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            var trainer = await _context.Trainers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.IsActive);

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays);
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                Token = newToken,
                RefreshToken = newRefreshToken,
                User = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Phone = user.Phone,
                    Email = user.Email,
                    Role = user.Role,
                    TelegramUsername = user.TelegramUsername,
                    IsTrainer = trainer != null,
                    TrainerId = trainer?.Id,
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                }
            };
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
        {
            var normalizedEmail = forgotPasswordDto.Email.Trim().ToLowerInvariant();
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email != null && EF.Functions.ILike(u.Email, normalizedEmail) && !u.IsDeleted);

            if (user == null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var activeTokens = await _context.PasswordResetTokens
                .Where(x => x.UserId == user.Id && x.UsedAt == null && x.ExpiresAt > now && !x.IsDeleted)
                .ToListAsync();

            foreach (var activeToken in activeTokens)
            {
                activeToken.IsDeleted = true;
                activeToken.UpdatedAt = now;
            }

            var token = GeneratePasswordResetToken();
            var tokenHash = HashPasswordResetToken(token);
            var resetToken = new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = tokenHash,
                ExpiresAt = now.AddMinutes(30),
                CreatedAt = now
            };

            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            var resetUrl = BuildPasswordResetUrl(token);
            await _emailService.SendPasswordResetEmailAsync(user.Email!, resetUrl);
        }

        public async Task ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
        {
            var tokenHash = HashPasswordResetToken(resetPasswordDto.Token);
            var now = DateTime.UtcNow;
            var resetToken = await _context.PasswordResetTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash && !x.IsDeleted);

            if (resetToken == null || resetToken.UsedAt != null || resetToken.ExpiresAt <= now)
            {
                throw new InvalidOperationException("Ссылка восстановления пароля недействительна или истекла");
            }

            if (resetToken.AttemptsCount >= 5)
            {
                resetToken.IsDeleted = true;
                resetToken.UpdatedAt = now;
                await _context.SaveChangesAsync();
                throw new InvalidOperationException("Превышено количество попыток восстановления пароля");
            }

            resetToken.AttemptsCount += 1;
            resetToken.UsedAt = now;
            resetToken.UpdatedAt = now;

            resetToken.User.PasswordHash = _passwordHasher.HashPassword(resetPasswordDto.NewPassword);
            resetToken.User.RefreshToken = null;
            resetToken.User.RefreshTokenExpiryTime = null;
            resetToken.User.UpdatedAt = now;

            await _context.SaveChangesAsync();
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto changePasswordDto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                throw new InvalidOperationException("Пользователь не найден");

            if (string.IsNullOrEmpty(user.PasswordHash) ||
                !_passwordHasher.VerifyPassword(user.PasswordHash, changePasswordDto.CurrentPassword))
                throw new UnauthorizedAccessException("Неверный текущий пароль");

            user.PasswordHash = _passwordHasher.HashPassword(changePasswordDto.NewPassword);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<UserDto> UpdateEmailAsync(Guid userId, UpdateEmailDto updateEmailDto)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw new InvalidOperationException("Пользователь не найден");

            var normalizedEmail = string.IsNullOrWhiteSpace(updateEmailDto.Email)
                ? null
                : updateEmailDto.Email.Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(normalizedEmail))
            {
                var emailBelongsToAnotherUser = await _context.Users.AnyAsync(u =>
                    u.Id != userId &&
                    !u.IsDeleted &&
                    u.Email != null &&
                    EF.Functions.ILike(u.Email, normalizedEmail));

                if (emailBelongsToAnotherUser)
                {
                    throw new InvalidOperationException("Пользователь с таким email уже существует");
                }
            }

            user.Email = normalizedEmail;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var trainer = await _context.Trainers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.IsActive);

            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Phone = user.Phone,
                Email = user.Email,
                Role = user.Role,
                TelegramUsername = user.TelegramUsername,
                IsTrainer = trainer != null,
                TrainerId = trainer?.Id,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }

        public async Task<UserDto> UpdateTelegramUsernameAsync(Guid userId, UpdateTelegramUsernameDto updateTelegramUsernameDto)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw new InvalidOperationException("Пользователь не найден");

            var normalizedTelegramUsername = NormalizeTelegramUsername(updateTelegramUsernameDto.TelegramUsername);
            var withAt = "@" + normalizedTelegramUsername;

            var usernameBelongsToAnotherUser = await _context.Users.AnyAsync(u =>
                u.Id != userId &&
                !u.IsDeleted &&
                (EF.Functions.ILike(u.TelegramUsername, normalizedTelegramUsername) ||
                 EF.Functions.ILike(u.TelegramUsername, withAt)));

            if (usernameBelongsToAnotherUser)
            {
                throw new InvalidOperationException("Пользователь с таким Telegram username уже существует");
            }

            var oldTelegramUsername = NormalizeTelegramUsername(user.TelegramUsername);
            var usernameChanged = !string.Equals(oldTelegramUsername, normalizedTelegramUsername, StringComparison.OrdinalIgnoreCase);

            user.TelegramUsername = normalizedTelegramUsername;
            user.UpdatedAt = DateTime.UtcNow;

            var bindings = await _context.TelegramChatBindings
                .Where(x => x.UserId == userId && !x.IsDeleted)
                .ToListAsync();

            foreach (var binding in bindings)
            {
                binding.TelegramUsername = normalizedTelegramUsername;
                binding.UpdatedAt = DateTime.UtcNow;

                if (usernameChanged)
                {
                    binding.IsActive = false;
                    binding.LastVerifiedAt = null;
                }
            }

            if (usernameChanged)
            {
                user.TelegramChatId = null;
            }

            await _context.SaveChangesAsync();

            var trainer = await _context.Trainers
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == user.Id && t.IsActive);

            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Phone = user.Phone,
                Email = user.Email,
                Role = user.Role,
                TelegramUsername = user.TelegramUsername,
                IsTrainer = trainer != null,
                TrainerId = trainer?.Id,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };
        }

        public async Task LogoutAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<User?> GetUserByPhone(string phoneNumber)
        {
            var trimmedNumber = phoneNumber.Replace(" ", "");            
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Phone == trimmedNumber);
            if(user != null)
            {
                return user;
            }
            return null;
        } 

        private static string NormalizeTelegramUsername(string telegramUsername)
        {
            return telegramUsername.Trim().TrimStart('@');
        }

        private static string GeneratePasswordResetToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-", StringComparison.Ordinal)
                .Replace("/", "_", StringComparison.Ordinal)
                .TrimEnd('=');
        }

        private static string HashPasswordResetToken(string token)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
            return Convert.ToHexString(hash);
        }

        private string BuildPasswordResetUrl(string token)
        {
            var separator = _frontendSettings.ResetPasswordUrl.Contains('?', StringComparison.Ordinal) ? "&" : "?";
            return $"{_frontendSettings.ResetPasswordUrl}{separator}token={HttpUtility.UrlEncode(token)}";
        }
    }
}
