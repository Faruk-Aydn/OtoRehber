using Microsoft.AspNetCore.Identity;

namespace OtoRehber.Domain.Entities
{
    // Ekstra bir alan gerekmiyor; Identity'nin IdentityUser'ı yeterli.
    // (Eski Level/XP/AvatarUrl alanları hiç kullanılmadığı için kaldırıldı — migration DropAppUserGamification.)
    public class AppUser : IdentityUser
    {
    }
}
