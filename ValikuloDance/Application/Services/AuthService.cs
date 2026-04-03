using Microsoft.EntityFrameworkCore;
using ValikuloDance.Domain.Entities;
using ValikuloDance.Infrastructure.Data;
using Microsoft.Extensions.Options;
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
        private readonly JwtSettings _jwtSettings;

        public AuthService(
            AppDbContext context,
            ITokenService tokenService,
            IPasswordHasher passwordHasher,
            IOptions<JwtSettings> jwtSettings)
        {
            _context = context;
            _tokenService = tokenService;
            _passwordHasher = passwordHasher;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Phone == registerDto.Phone);

            if (existingUser != null)
                throw new InvalidOperationException("Пользователь с таким номером телефона уже существует");

            var passwordHash = _passwordHasher.HashPassword(registerDto.Password);

            var user = new User
            {
                Id = Guid.NewGuid(),
                Name = registerDto.Name,
                Phone = registerDto.Phone,
                Email = registerDto.Email ?? string.Empty,
                TelegramUsername = registerDto.TelegramUsername,
                TelegramChatId = registerDto.TelegramUsername,
                Role = "Client",
                CreatedAt = DateTime.UtcNow,
                PasswordHash = passwordHash,
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

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

            // Генерируем токены
            var token = _tokenService.GenerateToken(user);
            Console.WriteLine("TOKEN: " + token);
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
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                }
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenDto refreshTokenDto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshTokenDto.RefreshToken);

            if (user == null)
                throw new UnauthorizedAccessException("Неверный refresh token");

            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh token истек");

            var newToken = _tokenService.GenerateToken(user);
            var newRefreshToken = _tokenService.GenerateRefreshToken();

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
                    CreatedAt = user.CreatedAt,
                    LastLoginAt = user.LastLoginAt
                }
            };
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
    }
}