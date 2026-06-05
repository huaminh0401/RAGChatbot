namespace RAGChatbotMVC.DTOs
{
    public class DocumentDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public int SubjectId { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}