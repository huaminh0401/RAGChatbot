using System.ComponentModel.DataAnnotations;

namespace RAGChatbotMVC.Models;

public class Subject
{
    public int Id { get; set; }
    [Required, StringLength(255)] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Document> Documents { get; set; } = new List<Document>();
}
