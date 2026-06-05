using System.ComponentModel.DataAnnotations;

namespace RAGChatbotMVC.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui long nhap email hoac ten dang nhap.")]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui long nhap mat khau.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class UserCreateViewModel
{
    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string UserName { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Role { get; set; } = UserRoles.Student;

    [Required, MinLength(6)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}

public class UserEditViewModel
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.Student;
    public bool IsActive { get; set; }
    public string? NewPassword { get; set; }
}
