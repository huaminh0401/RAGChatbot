using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAGChatbotMVC.Data;
using RAGChatbotMVC.Helpers;
using RAGChatbotMVC.Models;

namespace RAGChatbotMVC.Controllers;

[Authorize]
public class SubjectsController : Controller
{
    private readonly AppDbContext _db;
    public SubjectsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index() => View(await _db.Subjects.Include(s => s.Documents).ToListAsync());

    [Authorize(Roles = "Teacher,Admin")]
    public IActionResult Create() => View(new Subject());

    [Authorize(Roles = "Teacher,Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Subject subject)
    {
        if (!ModelState.IsValid) return View(subject);

        subject.Name = VietnameseText.ForDisplay(subject.Name);
        subject.Description = VietnameseText.ForDisplay(subject.Description);
        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
