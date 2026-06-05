using System.Text;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace RAGChatbotMVC.Services;

public class FileTextExtractor : IFileTextExtractor
{
    public Task<string> ExtractAsync(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        string text = ext switch
        {
            ".pdf" => ExtractPdf(filePath),
            ".docx" => ExtractDocx(filePath),
            ".pptx" => ExtractPptx(filePath),
            ".txt" => File.ReadAllText(filePath),
            _ => throw new NotSupportedException("Chỉ hỗ trợ PDF, DOCX, PPTX, TXT")
        };
        return Task.FromResult(text);
    }

    private static string ExtractPdf(string path)
    {
        var sb = new StringBuilder();
        using var doc = PdfDocument.Open(path);
        foreach (var page in doc.GetPages()) sb.AppendLine(page.Text);
        return sb.ToString();
    }

    private static string ExtractDocx(string path)
    {
        using var doc = WordprocessingDocument.Open(path, false);
        return doc.MainDocumentPart?.Document.Body?.InnerText ?? string.Empty;
    }

    private static string ExtractPptx(string path)
    {
        var sb = new StringBuilder();
        using var ppt = PresentationDocument.Open(path, false);
        foreach (var slide in ppt.PresentationPart!.SlideParts) sb.AppendLine(slide.Slide.InnerText);
        return sb.ToString();
    }
}
