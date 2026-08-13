using Microsoft.AspNetCore.Identity;

namespace OtoRehber.Domain.Entities
{
    public class AppUser : IdentityUser
    {
        public string? AvatarUrl { get; set; }
        public int Level { get; set; } = 1;
        public int XP { get; set; } = 0;
    }
}
