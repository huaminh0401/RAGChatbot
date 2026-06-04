namespace RAGChatbotMVC.Services;
public interface IChunkService { List<string> Split(string text, int maxWords = 160); }
