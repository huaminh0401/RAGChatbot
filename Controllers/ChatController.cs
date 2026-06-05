using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RAGChatbotMVC.Data;
using RAGChatbotMVC.Models;
using RAGChatbotMVC.Services;

namespace RAGChatbotMVC.Controllers;

public class ChatController : Controller
{
    private const string HistoryKey = "RagChatHistory";
    private readonly AppDbContext _db;
    private readonly IRagChatService _rag;

    public ChatController(AppDbContext db, IRagChatService rag)
    {
        _db = db;
        _rag = rag;
    }

    public async Task<IActionResult> Index()
    {
        return View(new ChatAskViewModel
        {
            Subjects = await SubjectOptions(),
            History = GetHistory()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask(ChatAskViewModel vm)
    {
        vm.Subjects = await SubjectOptions();
        vm.History = GetHistory();

        if (string.IsNullOrWhiteSpace(vm.Question))
        {
            ModelState.AddModelError(nameof(vm.Question), "Vui lòng nhập câu hỏi.");
            return View("Index", vm);
        }

        vm.Result = await _rag.AskAsync(vm.Question.Trim(), vm.SubjectId, vm.ModelName, vm.TopK);
        SaveHistory(vm.Result);
        vm.History = GetHistory();
        return View("Index", vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ClearHistory()
    {
        HttpContext.Session.Remove(HistoryKey);
        TempData["Success"] = "Đã xóa lịch sử hội thoại trong phiên hiện tại.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> SubjectOptions() => await _db.Subjects.OrderBy(s => s.Name)
        .Select(s => new SelectListItem(s.Name, s.Id.ToString())).ToListAsync();

    private List<ChatHistoryItem> GetHistory()
    {
        var json = HttpContext.Session.GetString(HistoryKey);
        if (string.IsNullOrWhiteSpace(json)) return new List<ChatHistoryItem>();
        try { return JsonSerializer.Deserialize<List<ChatHistoryItem>>(json) ?? new List<ChatHistoryItem>(); }
        catch { return new List<ChatHistoryItem>(); }
    }

    private void SaveHistory(ChatAnswerViewModel result)
    {
        var history = GetHistory();
        history.Insert(0, new ChatHistoryItem { Question = result.Question, Answer = result.Answer, AskedAt = result.AskedAt });
        history = history.Take(10).ToList();
        HttpContext.Session.SetString(HistoryKey, JsonSerializer.Serialize(history));
    }
}
