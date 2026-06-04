using System.ComponentModel.DataAnnotations;

namespace RAGChatbotMVC.Models;

public class EmbeddingResearch
{
    public int Id { get; set; }
    [Required] public int ChunkId { get; set; }
    [StringLength(100)] public string? ModelName { get; set; }
    [Required] public string VectorData { get; set; } = "[]";
    public double? ProcessingTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DocumentChunk? Chunk { get; set; }
}
