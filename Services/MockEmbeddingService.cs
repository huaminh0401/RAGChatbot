using System.Diagnostics;
using System.Text.Json;

namespace RAGChatbotMVC.Services;

public class MockEmbeddingService : IEmbeddingService
{
    public (string vectorJson, float elapsedMs) Embed(string text, string modelName)
    {
        var sw = Stopwatch.StartNew();
        var seed = Math.Abs(HashCode.Combine(text, modelName));
        var random = new Random(seed);
        var vector = Enumerable.Range(0, 12).Select(_ => Math.Round(random.NextDouble(), 5)).ToArray();
        sw.Stop();
        return (JsonSerializer.Serialize(vector), (float)sw.Elapsed.TotalMilliseconds);
    }
}
