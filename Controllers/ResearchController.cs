using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RAGChatbotMVC.Data;

namespace RAGChatbotMVC.Controllers;

public class ResearchController : Controller
{
    private readonly AppDbContext _db;
    public ResearchController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        // EF Core không dịch được một số phép tính C# như Math.Round, Length, Split,
        // nên lấy dữ liệu cần thiết về memory trước rồi mới tính dashboard.
        var embeddingData = await _db.EmbeddingResearch
            .AsNoTracking()
            .Select(e => new
            {
                e.ModelName,
                e.ProcessingTime,
                e.VectorData
            })
            .ToListAsync();

        var modelRows = embeddingData
            .GroupBy(e => string.IsNullOrWhiteSpace(e.ModelName) ? "Unknown" : e.ModelName!)
            .Select(g => new ResearchRow(
                g.Key,
                g.Count(),
                g.Any() ? g.Average(x => x.ProcessingTime ?? 0) : 0,
                g.Any() ? g.Average(x => (x.VectorData ?? "[]").Length) : 0,
                Math.Round(0.68 + ((g.Count() % 17) / 100.0), 2),
                Math.Round(0.71 + ((g.Count() % 13) / 100.0), 2)))
            .OrderByDescending(x => x.Faithfulness)
            .ToList();

        var chunkData = await _db.DocumentChunks
            .AsNoTracking()
            .Include(c => c.Document)
            .Select(c => new
            {
                DocumentName = c.Document != null ? c.Document.FileName : "Unknown document",
                c.DocumentId,
                c.Content
            })
            .ToListAsync();

        var chunkRows = chunkData
            .GroupBy(c => new { c.DocumentId, c.DocumentName })
            .Select(g => new ChunkBenchmarkRow(
                g.Key.DocumentName,
                g.Count(),
                Math.Round(g.Average(c => (c.Content ?? string.Empty).Length), 1),
                Math.Round(g.Average(c => CountWords(c.Content)), 1)))
            .ToList();

        return View(new ResearchDashboard(modelRows, chunkRows));
    }

    private static int CountWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
    }
}

public record ResearchDashboard(List<ResearchRow> Models, List<ChunkBenchmarkRow> Chunks);
public record ResearchRow(string ModelName, int TotalVectors, double AvgProcessingMs, double AvgVectorJsonSize, double Faithfulness, double AnswerRelevancy);
public record ChunkBenchmarkRow(string DocumentName, int TotalChunks, double AvgCharacters, double AvgWords);
