using Microsoft.AspNetCore.Mvc.Rendering;

namespace RAGChatbotMVC.Models;

public class ChatAskViewModel
{
    public string? Question { get; set; }
    public int? SubjectId { get; set; }
    public string ModelName { get; set; } = "bge-m3";
    public int TopK { get; set; } = 3;
    public List<SelectListItem> Subjects { get; set; } = new();
    public ChatAnswerViewModel? Result { get; set; }
    public List<ChatHistoryItem> History { get; set; } = new();
}

public class ChatAnswerViewModel
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public List<RetrievedSourceViewModel> Sources { get; set; } = new();
    public DateTime AskedAt { get; set; } = DateTime.Now;
}

public class RetrievedSourceViewModel
{
    public int ChunkId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int? ChunkOrder { get; set; }
    public double Score { get; set; }
    public string Preview { get; set; } = string.Empty;
}

public class ChatHistoryItem
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public DateTime AskedAt { get; set; } = DateTime.Now;
}
