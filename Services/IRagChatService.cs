using RAGChatbotMVC.Models;

namespace RAGChatbotMVC.Services;

public interface IRagChatService
{
    Task<ChatAnswerViewModel> AskAsync(string question, int? subjectId, string modelName, int topK);
}
