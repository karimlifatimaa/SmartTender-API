using System.ComponentModel.DataAnnotations;

namespace SmartTender.Application.DTOs
{
    public class RegisterDto
    {
        [Required]
        public string UserName { get; set; }
        
        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
        
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        
        [Required]
        public string? FullName { get; set; }
        
        [Required]
        public string? Voen { get; set; }
    }
} 