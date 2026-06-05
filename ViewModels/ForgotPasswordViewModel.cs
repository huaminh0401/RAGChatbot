using System.ComponentModel.DataAnnotations;

namespace RAGChatbotMVC.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        public string Email { get; set; }
    }
}