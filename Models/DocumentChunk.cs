using System.ComponentModel.DataAnnotations;

namespace RAGChatbotMVC.Models;

public class DocumentChunk
{
    public int Id { get; set; }
    [Required] public int DocumentId { get; set; }
    [Required] public string Content { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public int? ChunkOrder { get; set; }
    public string? Metadata { get; set; }
    public Document? Document { get; set; }
    public ICollection<EmbeddingResearch> Embeddings { get; set; } = new List<EmbeddingResearch>();
}
