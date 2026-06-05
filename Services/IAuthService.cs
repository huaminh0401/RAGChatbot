using RAGChatbotMVC.DTOs;
using RAGChatbotMVC.Models;

namespace RAGChatbotMVC.Services;

public interface IAuthService
{
    Task<AuthResponseDTO?> LoginAsync(string userNameOrEmail, string password);
    Task<AuthResponseDTO?> RefreshAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string? refreshToken);
    Task<ApplicationUser> CreateUserAsync(UserCreateViewModel input);
    Task SeedDefaultUsersAsync();
}
