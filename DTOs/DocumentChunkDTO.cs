namespace RAGChatbotMVC.DTOs
{
    public class DocumentChunkDTO
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public string ChunkText { get; set; } = string.Empty;
    }
}