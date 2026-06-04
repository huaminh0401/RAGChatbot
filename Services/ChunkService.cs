namespace RAGChatbotMVC.Services;
public class ChunkService : IChunkService
{
    public List<string> Split(string text, int maxWords = 160)
    {
        var words = text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();
        for (int i = 0; i < words.Length; i += maxWords)
            result.Add(string.Join(' ', words.Skip(i).Take(maxWords)));
        return result.Count == 0 ? new List<string>{"Không đọc được nội dung tài liệu."} : result;
    }
}
