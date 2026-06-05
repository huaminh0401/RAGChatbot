using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace RAGChatbotMVC.Models;

public class DocumentUploadViewModel
{
    public int? SubjectId { get; set; }
    public string? NewSubjectName { get; set; }
    public string? OriginalFileName { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn file cần upload.")]
    public IFormFile? File { get; set; }

    public List<SelectListItem> Subjects { get; set; } = new();
}
