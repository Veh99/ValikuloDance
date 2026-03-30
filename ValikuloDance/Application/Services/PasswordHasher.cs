using System.Security.Cryptography;
using System.Text;
using ValikuloDance.Application.Interfaces;

namespace ValikuloDance.Application.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        public bool VerifyPassword(string hash, string password)
        {
            var hashedPassword = HashPassword(password);
            return hash == hashedPassword;
        }
    }
}
