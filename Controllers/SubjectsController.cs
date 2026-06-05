using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAGChatbotMVC.Data;
using RAGChatbotMVC.Models;

namespace RAGChatbotMVC.Controllers;
[Authorize(Roles = UserRoles.StudentTeacherAdmin)]
public class SubjectsController : Controller
{
    private readonly AppDbContext _db;
    public SubjectsController(AppDbContext db) => _db = db;
    public async Task<IActionResult> Index() => View(await _db.Subjects.Include(s => s.Documents).ToListAsync());
    [Authorize(Roles = UserRoles.TeacherAdmin)]
    public IActionResult Create() => View(new Subject());
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = UserRoles.TeacherAdmin)]
    public async Task<IActionResult> Create(Subject subject)
    { if (!ModelState.IsValid) return View(subject); _db.Subjects.Add(subject); await _db.SaveChangesAsync(); return RedirectToAction(nameof(Index)); }
}
