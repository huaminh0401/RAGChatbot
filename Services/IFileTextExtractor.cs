namespace RAGChatbotMVC.Services;
public interface IFileTextExtractor { Task<string> ExtractAsync(string filePath); }
