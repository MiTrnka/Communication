using Microsoft.AspNetCore.Identity;

namespace APIIdentity.Models;

public class ApplicationUser : IdentityUser
{
    // Rozšíření standardního IdentityUser o vlastní pole
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;

    // Pro produkci zvážit přidání dalších polí jako:
    // - LastLoginDate
    // - ProfilePicture
    // - TwoFactorEnabled
    // - PreferredLanguage
}
