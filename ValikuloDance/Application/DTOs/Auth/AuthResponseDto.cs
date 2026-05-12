namespace ValikuloDance.Application.DTOs.Auth
{
    public class AuthResponseDto
    {
        public string Token { get; set; }
        public string AccessToken
        {
            get => Token;
            set => Token = value;
        }

        public string RefreshToken { get; set; }
        public UserDto User { get; set; }
    }
}
