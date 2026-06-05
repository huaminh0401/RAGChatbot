
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAGChatbotMVC.Data;
using RAGChatbotMVC.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace RAGChatbotMVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            // Kiểm tra tài khoản không tồn tại
            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản không tồn tại. Vui lòng đăng ký.");
                return View();
            }

            // Kiểm tra tài khoản bị khóa
            if (user.IsLocked)
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ admin.");
                return View();
            }

            // Kiểm tra mật khẩu
            if (VerifyPassword(password, user.PasswordHash))
            {
                // Tạo claims bao gồm Role
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Role, user.Role ?? "User")  // Gán role
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                return RedirectToAction("Index", "Documents");
            }

            ModelState.AddModelError("", "Sai mật khẩu.");
            return View();
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User model, string password)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (existingUser == null)
                {
                    model.PasswordHash = HashPassword(password);
                    model.Role = "User";        // Mặc định tài khoản thường
                    model.IsLocked = false;     // Không bị khóa
                    _context.Users.Add(model);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Login");
                }
                ModelState.AddModelError("Email", "Email đã tồn tại.");
            }
            return View(model);
        }

        // GET: /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            // Ở đây bạn có thể gửi email reset. Tạm thời thông báo chung.
            TempData["Message"] = "Nếu email tồn tại, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu.";
            return View();
        }

        // GET: /Account/Logout
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        // Hàm mã hóa mật khẩu (SHA256)
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        // Hàm kiểm tra mật khẩu
        private bool VerifyPassword(string password, string storedHash)
        {
            return HashPassword(password) == storedHash;
        }
    }
}