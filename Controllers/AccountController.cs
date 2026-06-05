using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAGChatbotMVC.Data;
using RAGChatbotMVC.Models;
using RAGChatbotMVC.ViewModels;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RAGChatbotMVC.Controllers;

public class AccountController : Controller
{
    private static readonly HashSet<string> RegisterableRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Student",
        "Teacher"
    };

    private readonly AppDbContext _context;

    public AccountController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Documents");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("", "Vui lòng nhập email và mật khẩu.");
            return View();
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null)
        {
            ModelState.AddModelError("", "Tài khoản không tồn tại. Vui lòng đăng ký.");
            return View();
        }

        if (user.IsLocked)
        {
            ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Admin.");
            return View();
        }

        if (!user.IsApproved)
        {
            ModelState.AddModelError("", "Tài khoản đang chờ Admin duyệt trước khi đăng nhập.");
            return View();
        }

        if (!VerifyPassword(password, user.PasswordHash))
        {
            ModelState.AddModelError("", "Sai mật khẩu.");
            return View();
        }

        var role = NormalizeRole(user.Role);
        var displayName = string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName;
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim("FullName", displayName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

        return RedirectToAction("Index", "Documents");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Documents");
        }

        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!RegisterableRoles.Contains(model.RequestedRole))
        {
            ModelState.AddModelError(nameof(model.RequestedRole), "Vai trò đăng ký không hợp lệ.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim().ToLowerInvariant();
        var existingUser = await _context.Users.AnyAsync(u => u.Email == normalizedEmail);
        if (existingUser)
        {
            ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại.");
            return View(model);
        }

        var hasAdmin = await _context.Users.AnyAsync(u => u.Role == "Admin");
        var user = new User
        {
            FullName = model.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = HashPassword(model.Password),
            Role = hasAdmin ? NormalizeRole(model.RequestedRole) : "Admin",
            IsApproved = !hasAdmin,
            IsLocked = false,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        TempData["Message"] = !hasAdmin
            ? "Tài khoản Admin đầu tiên đã được tạo. Bạn có thể đăng nhập."
            : "Đăng ký thành công. Vui lòng chờ Admin duyệt tài khoản.";

        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userId, out var id)) return RedirectToAction(nameof(Login));

        var user = await _context.Users.FindAsync(id);
        if (user == null) return RedirectToAction(nameof(Login));

        return View(user);
    }

    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        TempData["Message"] = "Bạn không có quyền truy cập chức năng này.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        _ = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        TempData["Message"] = "Nếu email tồn tại, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu.";
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private static string NormalizeRole(string? role)
    {
        return role?.Trim().ToLowerInvariant() switch
        {
            "admin" => "Admin",
            "teacher" => "Teacher",
            _ => "Student"
        };
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static bool VerifyPassword(string password, string storedHash)
    {
        return HashPassword(password) == storedHash;
    }
}
