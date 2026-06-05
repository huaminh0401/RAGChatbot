using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RAGChatbotMVC.Data;
using RAGChatbotMVC.Models;
using RAGChatbotMVC.Services;

namespace RAGChatbotMVC.Controllers;

[Authorize(Roles = UserRoles.StudentTeacherAdmin)]
public class DocumentsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly IFileTextExtractor _extractor;
    private readonly IChunkService _chunker;
    private readonly IEmbeddingService _embedding;

    public DocumentsController(AppDbContext db, IWebHostEnvironment env, IFileTextExtractor extractor, IChunkService chunker, IEmbeddingService embedding)
    { _db = db; _env = env; _extractor = extractor; _chunker = chunker; _embedding = embedding; }

    public async Task<IActionResult> Index()
    {
        var docs = await _db.Documents.Include(d => d.Subject).Include(d => d.Chunks)
            .OrderByDescending(d => d.UploadedAt).ToListAsync();
        return View(docs);
    }

    [Authorize(Roles = UserRoles.TeacherAdmin)]
    public async Task<IActionResult> Upload()
    {
        await EnsureDefaultSubject();
        return View(new DocumentUploadViewModel { Subjects = await SubjectOptions() });
    }

    [Authorize(Roles = UserRoles.TeacherAdmin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(DocumentUploadViewModel vm)
    {
        if (!ModelState.IsValid) { vm.Subjects = await SubjectOptions(); return View(vm); }

        if (!string.IsNullOrWhiteSpace(vm.NewSubjectName))
        {
            var name = vm.NewSubjectName.Trim();
            var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Name == name);
            if (subject == null)
            {
                subject = new Subject { Name = name, Description = "Tạo khi upload tài liệu" };
                _db.Subjects.Add(subject);
                await _db.SaveChangesAsync();
            }
            vm.SubjectId = subject.Id;
        }

        if (vm.SubjectId == null)
        {
            ModelState.AddModelError("SubjectId", "Vui lòng chọn môn học hoặc nhập môn học mới.");
            vm.Subjects = await SubjectOptions();
            return View(vm);
        }

        var allowed = new[] { ".pdf", ".docx", ".pptx", ".txt" };
        var ext = Path.GetExtension(vm.File!.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext)) { ModelState.AddModelError("File", "File không đúng định dạng. Chỉ hỗ trợ PDF, DOCX, PPTX, TXT."); vm.Subjects = await SubjectOptions(); return View(vm); }

        var uploadDir = Path.Combine(_env.WebRootPath, "uploads");
        Directory.CreateDirectory(uploadDir);
        var safeName = $"{Guid.NewGuid():N}{ext}";
        var path = Path.Combine(uploadDir, safeName);
        await using (var stream = System.IO.File.Create(path)) await vm.File.CopyToAsync(stream);

        var document = new Document { SubjectId = vm.SubjectId.Value, FileName = vm.File.FileName, FilePath = "/uploads/" + safeName, FileType = ext.Trim('.') };
        _db.Documents.Add(document);
        await _db.SaveChangesAsync();

        var text = await _extractor.ExtractAsync(path);
        var chunks = _chunker.Split(text);
        var modelNames = new[] { "multilingual-e5-base", "text-embedding-3-small", "PhoBERT-base", "bge-m3" };
        for (int i = 0; i < chunks.Count; i++)
        {
            var chunk = new DocumentChunk { DocumentId = document.Id, Content = chunks[i], ChunkOrder = i + 1, Metadata = $"{{\"source\":\"{document.FileName}\"}}" };
            _db.DocumentChunks.Add(chunk);
            await _db.SaveChangesAsync();
            foreach (var model in modelNames)
            {
                var embedded = _embedding.Embed(chunks[i], model);
                _db.EmbeddingResearch.Add(new EmbeddingResearch { ChunkId = chunk.Id, ModelName = model, VectorData = embedded.vectorJson, ProcessingTime = embedded.elapsedMs });
            }
        }
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Details), new { id = document.Id });
    }

    public async Task<IActionResult> Details(int id)
    {
        var doc = await _db.Documents.Include(d => d.Subject).Include(d => d.Chunks).ThenInclude(c => c.Embeddings)
            .FirstOrDefaultAsync(d => d.Id == id);
        return doc == null ? NotFound() : View(doc);
    }

    [Authorize(Roles = UserRoles.TeacherAdmin)]
    public async Task<IActionResult> Edit(int id)
    {
        var doc = await _db.Documents.FindAsync(id);
        if (doc == null) return NotFound();
        ViewBag.Subjects = await SubjectOptions();
        return View(doc);
    }

    [Authorize(Roles = UserRoles.TeacherAdmin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Document input)
    {
        if (id != input.Id) return BadRequest();

        var doc = await _db.Documents.FindAsync(id);
        if (doc == null) return NotFound();

        if (string.IsNullOrWhiteSpace(input.FileName))
        {
            ModelState.AddModelError("FileName", "Tên tài liệu không được để trống.");
        }

        if (!await _db.Subjects.AnyAsync(s => s.Id == input.SubjectId))
        {
            ModelState.AddModelError("SubjectId", "Môn học không hợp lệ.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Subjects = await SubjectOptions();
            return View(input);
        }

        doc.FileName = input.FileName.Trim();
        doc.SubjectId = input.SubjectId;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã cập nhật thông tin tài liệu.";
        return RedirectToAction(nameof(Details), new { id = doc.Id });
    }

    [Authorize(Roles = UserRoles.Admin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var doc = await _db.Documents.Include(d => d.Chunks).ThenInclude(c => c.Embeddings)
            .FirstOrDefaultAsync(d => d.Id == id);
        if (doc == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(doc.FilePath))
        {
            var relative = doc.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var physicalPath = Path.Combine(_env.WebRootPath, relative);
            if (System.IO.File.Exists(physicalPath)) System.IO.File.Delete(physicalPath);
        }

        _db.Documents.Remove(doc);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Đã xóa tài liệu.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> SubjectOptions() => await _db.Subjects.OrderBy(s => s.Name)
        .Select(s => new SelectListItem(s.Name, s.Id.ToString())).ToListAsync();

    private async Task EnsureDefaultSubject()
    {
        if (!await _db.Subjects.AnyAsync()) { _db.Subjects.Add(new Subject { Name = "Demo môn học", Description = "Môn mẫu để demo workflow upload tài liệu" }); await _db.SaveChangesAsync(); }
    }
}
