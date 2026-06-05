namespace RAGChatbotMVC.DTOs
{
    public class ResearchResponseDTO
    {
        public string Answer { get; set; } = string.Empty;
        public List<string> Sources { get; set; } = new();
    }
}