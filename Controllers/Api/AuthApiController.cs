using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RAGChatbotMVC.DTOs;
using RAGChatbotMVC.Services;

namespace RAGChatbotMVC.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthApiController(IAuthService authService) => _authService = authService;

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDTO request)
    {
        var response = await _authService.LoginAsync(request.UserNameOrEmail, request.Password);
        return response == null ? Unauthorized(new { message = "Invalid credentials." }) : Ok(response);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequestDTO request)
    {
        var response = await _authService.RefreshAsync(request.RefreshToken);
        return response == null ? Unauthorized(new { message = "Invalid refresh token." }) : Ok(response);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequestDTO request)
    {
        var refreshToken = request.RefreshToken;
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            Request.Cookies.TryGetValue("RefreshToken", out refreshToken);
        }

        await _authService.RevokeRefreshTokenAsync(refreshToken);
        return NoContent();
    }
}
