using System.ComponentModel.DataAnnotations;

namespace RAGChatbotMVC.Models;

public class ApplicationUser
{
    public int Id { get; set; }

    [Required, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string UserName { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Role { get; set; } = UserRoles.Student;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
