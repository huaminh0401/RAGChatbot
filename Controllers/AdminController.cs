using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAGChatbotMVC.Data;
using RAGChatbotMVC.Models;
using System.Security.Cryptography;
using System.Text;

namespace RAGChatbotMVC.Controllers;

[Authorize(Roles = "Admin")]  // chỉ admin mới vào được
public class AdminController : Controller
{
    private readonly AppDbContext _context;

    public AdminController(AppDbContext context)
    {
        _context = context;
    }

    // Danh sách tài khoản
    public async Task<IActionResult> Index()
    {
        var users = await _context.Users.ToListAsync();
        return View(users);
    }

    // Chỉnh sửa quyền (Role) và trạng thái khóa
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string role, bool isLocked)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.Role = role;
        user.IsLocked = isLocked;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Cập nhật quyền thành công.";
        return RedirectToAction(nameof(Index));
    }

    // Xóa tài khoản (chỉ admin mới xóa được)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound();

        // Không cho xóa chính mình
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
}