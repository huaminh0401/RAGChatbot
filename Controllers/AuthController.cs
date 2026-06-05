using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RAGChatbotMVC.Models;
using RAGChatbotMVC.Services;

namespace RAGChatbotMVC.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly JwtOptions _jwtOptions;

    public AuthController(IAuthService authService, IOptions<JwtOptions> jwtOptions)
    {
        _authService = authService;
        _jwtOptions = jwtOptions.Value;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var response = await _authService.LoginAsync(vm.UserNameOrEmail, vm.Password);
        if (response == null)
        {
            ModelState.AddModelError(string.Empty, "Thong tin dang nhap khong dung hoac tai khoan da bi khoa.");
            return View(vm);
        }

        WriteAuthCookies(response.AccessToken, response.RefreshToken, response.ExpiresAt);
        return RedirectToLocal(vm.ReturnUrl);
    }

    [Authorize]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        Request.Cookies.TryGetValue("RefreshToken", out var refreshToken);
        await _authService.RevokeRefreshTokenAsync(refreshToken);
        Response.Cookies.Delete("AccessToken");
        Response.Cookies.Delete("RefreshToken");
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet]
    public IActionResult Forbidden() => View();

    private void WriteAuthCookies(string accessToken, string refreshToken, DateTime expiresAt)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Strict
        };

        Response.Cookies.Append("AccessToken", accessToken, new CookieOptions
        {
            HttpOnly = cookieOptions.HttpOnly,
            Secure = cookieOptions.Secure,
            SameSite = cookieOptions.SameSite,
            Expires = expiresAt
        });

        Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
        {
            HttpOnly = cookieOptions.HttpOnly,
            Secure = cookieOptions.Secure,
            SameSite = cookieOptions.SameSite,
            Expires = DateTimeOffset.Now.AddDays(_jwtOptions.RefreshTokenDays)
        });
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Documents");
    }
}
