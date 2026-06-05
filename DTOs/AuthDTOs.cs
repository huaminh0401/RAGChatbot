namespace RAGChatbotMVC.DTOs;

public class LoginRequestDTO
{
    public string UserNameOrEmail { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequestDTO
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutRequestDTO
{
    public string? RefreshToken { get; set; }
}

public class AuthResponseDTO
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public AuthUserDTO User { get; set; } = new();
}

public class AuthUserDTO
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
