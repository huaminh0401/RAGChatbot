using System.ComponentModel.DataAnnotations;

namespace RAGChatbotMVC.Models;

public class User
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    public required string Email { get; set; }

    [Required]
    public required string PasswordHash { get; set; }

    public string? FullName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string Role { get; set; } = "Student";

    public bool IsLocked { get; set; } = false;

    public bool IsApproved { get; set; } = false;
}
