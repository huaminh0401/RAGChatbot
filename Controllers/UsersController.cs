using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAGChatbotMVC.Data;
using RAGChatbotMVC.Models;
using RAGChatbotMVC.Services;

namespace RAGChatbotMVC.Controllers;

[Authorize(Roles = UserRoles.Admin)]
public class UsersController : Controller
{
    private readonly AppDbContext _db;
    private readonly IAuthService _authService;
    private readonly PasswordHasher<ApplicationUser> _passwordHasher;

    public UsersController(AppDbContext db, IAuthService authService, PasswordHasher<ApplicationUser> passwordHasher)
    {
        _db = db;
        _authService = authService;
        _passwordHasher = passwordHasher;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _db.ApplicationUsers.OrderBy(u => u.Role).ThenBy(u => u.Email).ToListAsync();
        return View(users);
    }

    public IActionResult Create() => View(new UserCreateViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel vm)
    {
        if (!UserRoles.All.Contains(vm.Role))
        {
            ModelState.AddModelError(nameof(vm.Role), "Role khong hop le.");
        }

        if (!ModelState.IsValid) return View(vm);

        try
        {
            await _authService.CreateUserAsync(vm);
            TempData["Success"] = "Da tao tai khoan moi.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(vm);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var user = await _db.ApplicationUsers.FindAsync(id);
        if (user == null) return NotFound();

        return View(new UserEditViewModel
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            FullName = user.FullName,
            Role = user.Role,
            IsActive = user.IsActive
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserEditViewModel vm)
    {
        if (id != vm.Id) return BadRequest();
        if (!UserRoles.All.Contains(vm.Role)) ModelState.AddModelError(nameof(vm.Role), "Role khong hop le.");
        if (!string.IsNullOrWhiteSpace(vm.NewPassword) && vm.NewPassword.Length < 6)
        {
            ModelState.AddModelError(nameof(vm.NewPassword), "Mat khau moi can toi thieu 6 ky tu.");
        }
        if (!ModelState.IsValid) return View(vm);

        var user = await _db.ApplicationUsers.FindAsync(id);
        if (user == null) return NotFound();

        user.FullName = vm.FullName.Trim();
        user.Role = vm.Role;
        user.IsActive = vm.IsActive;
        if (!string.IsNullOrWhiteSpace(vm.NewPassword))
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, vm.NewPassword);
            await RevokeUserRefreshTokens(user.Id);
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Da cap nhat tai khoan.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var user = await _db.ApplicationUsers.FindAsync(id);
        if (user == null) return NotFound();

        user.IsActive = !user.IsActive;
        if (!user.IsActive)
        {
            await RevokeUserRefreshTokens(user.Id);
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = user.IsActive ? "Da mo khoa tai khoan." : "Da khoa tai khoan.";
        return RedirectToAction(nameof(Index));
    }

    private async Task RevokeUserRefreshTokens(int userId)
    {
        var activeTokens = await _db.RefreshTokens
            .Where(t => t.ApplicationUserId == userId && t.RevokedAt == null && t.ExpiresAt > DateTime.Now)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.Now;
        }
    }
}
