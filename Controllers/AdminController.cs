using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAGChatbotMVC.Data;

namespace RAGChatbotMVC.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin",
        "Teacher",
        "Student"
    };

    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _context.Users.OrderBy(u => u.IsApproved).ThenByDescending(u => u.CreatedAt).ToListAsync();
        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string role, bool isLocked, bool isApproved)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (!ValidRoles.Contains(role))
        {
            TempData["Error"] = "Vai trò không hợp lệ.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        user.Role = NormalizeRole(role);
        user.IsLocked = isLocked;
        user.IsApproved = isApproved;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Cập nhật tài khoản thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.IsApproved = true;
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Đã duyệt tài khoản {user.Email}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        var currentUserEmail = User.Identity!.Name;
        if (user.Email == currentUserEmail)
        {
            TempData["Error"] = "Bạn không thể xóa tài khoản của chính mình.";
            return RedirectToAction(nameof(Index));
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Đã xóa tài khoản.";
        return RedirectToAction(nameof(Index));
    }

    private static string NormalizeRole(string role)
    {
        return role.Trim().ToLowerInvariant() switch
        {
            "admin" => "Admin",
            "teacher" => "Teacher",
            _ => "Student"
        };
    }
}
