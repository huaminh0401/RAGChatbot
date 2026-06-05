using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RAGChatbotMVC.Data;
using RAGChatbotMVC.Models;

namespace RAGChatbotMVC.Services;

public class RagChatService : IRagChatService
{
    private readonly AppDbContext _db;
    private readonly IEmbeddingService _embedding;

    public RagChatService(AppDbContext db, IEmbeddingService embedding)
    {
        _db = db;
        _embedding = embedding;
    }

    public async Task<ChatAnswerViewModel> AskAsync(string question, int? subjectId, string modelName, int topK)
    {
        topK = Math.Clamp(topK, 1, 8);
        var query = _db.DocumentChunks
            .Include(c => c.Document)!.ThenInclude(d => d!.Subject)
            .Include(c => c.Embeddings)
            .AsQueryable();

        if (subjectId.HasValue)
            query = query.Where(c => c.Document != null && c.Document.SubjectId == subjectId.Value);

        var chunks = await query.ToListAsync();
        if (!chunks.Any())
        {
            return new ChatAnswerViewModel
            {
                Question = question,
                Answer = "Chưa có dữ liệu phù hợp. Hãy upload tài liệu hoặc chọn lại môn học trước khi hỏi.",
                Sources = new List<RetrievedSourceViewModel>()
            };
        }

        var qVec = ParseVector(_embedding.Embed(question, modelName).vectorJson);
        var ranked = chunks.Select(c =>
        {
            var modelVector = c.Embeddings.FirstOrDefault(e => e.ModelName == modelName)?.VectorData
                ?? c.Embeddings.FirstOrDefault()?.VectorData;
            var cVec = ParseVector(modelVector ?? "[]");
            var cosine = Cosine(qVec, cVec);
            var overlap = KeywordOverlap(question, c.Content);
            var score = (cosine * 0.55) + (overlap * 0.45);
            return new { Chunk = c, Score = score };
        })
        .OrderByDescending(x => x.Score)
        .Take(topK)
        .ToList();

        var sources = ranked.Select(x => new RetrievedSourceViewModel
        {
            ChunkId = x.Chunk.Id,
            DocumentName = x.Chunk.Document?.FileName ?? "Unknown document",
            SubjectName = x.Chunk.Document?.Subject?.Name ?? "Unknown subject",
            ChunkOrder = x.Chunk.ChunkOrder,
            Score = x.Score,
            Preview = Shorten(x.Chunk.Content, 340)
        }).ToList();

        var answer = BuildAnswer(question, ranked.Select(x => x.Chunk.Content).ToList(), sources);
        return new ChatAnswerViewModel { Question = question, Answer = answer, Sources = sources };
    }

    private static string BuildAnswer(string question, List<string> contexts, List<RetrievedSourceViewModel> sources)
    {
        var keywords = Tokenize(question).ToHashSet();
        var sentences = contexts
            .SelectMany(c => c.Split(new[] { '.', '!', '?', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            .Select(s => s.Trim())
            .Where(s => s.Length > 20)
            .Select(s => new { Sentence = s, Score = Tokenize(s).Count(keywords.Contains) })
            .OrderByDescending(x => x.Score)
            .Take(4)
            .Select(x => x.Sentence)
            .ToList();

        if (!sentences.Any())
            sentences = contexts.Take(2).Select(c => Shorten(c, 220)).ToList();

        var citationNames = string.Join(", ", sources.Select((s, i) => $"[{i + 1}] {s.DocumentName} - chunk {s.ChunkOrder}").Distinct());
        return $"Dựa trên các đoạn tài liệu tìm được, câu trả lời ngắn gọn là: {string.Join(" ", sentences)}.\n\nNguồn tham khảo: {citationNames}.";
    }

    private static double[] ParseVector(string json)
    {
        try { return JsonSerializer.Deserialize<double[]>(json) ?? Array.Empty<double>(); }
        catch { return Array.Empty<double>(); }
    }

    private static double Cosine(double[] a, double[] b)
    {
        if (a.Length == 0 || b.Length == 0) return 0;
        var n = Math.Min(a.Length, b.Length);
        double dot = 0, na = 0, nb = 0;
        for (var i = 0; i < n; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        return na == 0 || nb == 0 ? 0 : dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }

    private static double KeywordOverlap(string a, string b)
    {
        var qa = Tokenize(a).ToHashSet();
        var qb = Tokenize(b).ToHashSet();
        if (qa.Count == 0 || qb.Count == 0) return 0;
        return qa.Count(qb.Contains) / (double)qa.Count;
    }

    private static IEnumerable<string> Tokenize(string text) => text.ToLowerInvariant()
        .Split(new[] { ' ', '\n', '\r', '\t', '.', ',', ';', ':', '!', '?', '(', ')', '[', ']', '{', '}', '/', '\\', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
        .Where(t => t.Length > 2);

    private static string Shorten(string text, int max) => string.IsNullOrWhiteSpace(text)
        ? string.Empty
        : text.Length <= max ? text : text[..max] + "...";
}
