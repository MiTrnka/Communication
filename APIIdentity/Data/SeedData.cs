// Inicializace základních dat
// Vytváří role: Admin, Moderator, User
// Vytváří výchozí admin účet
using APIIdentity.Models;
using Microsoft.AspNetCore.Identity;

namespace APIIdentity.Data;

public static class SeedData
{
    public static async Task Initialize(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        // Vytvoření základních rolí
        string[] roles = { "Admin", "Moderator", "User" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Vytvoření admin účtu pokud neexistuje
        var adminEmail = "admin@example.com";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}