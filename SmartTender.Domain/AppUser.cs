using Microsoft;
using Microsoft.AspNetCore.Identity;

namespace SmartTender.Domain
{
    public class AppUser : IdentityUser
    {
        public string? FullName { get; set; }
        
        public string? Voen { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
} 