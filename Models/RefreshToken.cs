using System.ComponentModel.DataAnnotations;

namespace RAGChatbotMVC.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public int ApplicationUserId { get; set; }

    [Required, StringLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    [StringLength(128)]
    public string? ReplacedByTokenHash { get; set; }

    public ApplicationUser? ApplicationUser { get; set; }
    public bool IsActive => RevokedAt == null && ExpiresAt > DateTime.Now;
}
