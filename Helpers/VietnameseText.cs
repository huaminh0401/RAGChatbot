namespace RAGChatbotMVC.Helpers;

public static class VietnameseText
{
    public static string ForDisplay(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var text = value.Trim();

        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Truy?n c? tích"] = "Truyện cổ tích",
            ["Truy?n c? tich"] = "Truyện cổ tích",
            ["Truyen co tich"] = "Truyện cổ tích",
            ["Môn Van H?c"] = "Môn Văn Học",
            ["Mon Van H?c"] = "Môn Văn Học",
            ["Van H?c"] = "Văn Học",
            ["Van Hoc"] = "Văn Học",
            ["MLN111 -Bài dạng - Tác d?ng c?a chuy?n d?i s? d?n dào t?o M? thu?t s? t?i Vi?t Nam hi?n nay.docx"] = "MLN111 - Bài dạng - Tác động của chuyển đổi số đến đào tạo Mỹ thuật số tại Việt Nam hiện nay.docx",
            ["MLN111 -Bài dạng - Tác d?ng c?a chuy?n d?i s? d?n dào t?o M? thu?t s? t?i Vi?t Nam hi?n nay"] = "MLN111 - Bài dạng - Tác động của chuyển đổi số đến đào tạo Mỹ thuật số tại Việt Nam hiện nay"
        };

        if (replacements.TryGetValue(text, out var exact))
        {
            return exact;
        }

        foreach (var item in replacements)
        {
            text = text.Replace(item.Key, item.Value, StringComparison.OrdinalIgnoreCase);
        }

        return text;
    }
}
