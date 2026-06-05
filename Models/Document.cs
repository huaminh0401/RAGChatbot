using System.ComponentModel.DataAnnotations;

namespace RAGChatbotMVC.Models;

public class Document
{
    public int Id { get; set; }
    [Required] public int SubjectId { get; set; }
    [Required, StringLength(500)] public string FileName { get; set; } = string.Empty;
    [StringLength(1000)] public string? FilePath { get; set; }
    [StringLength(50)] public string? FileType { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.Now;
    public Subject? Subject { get; set; }
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
