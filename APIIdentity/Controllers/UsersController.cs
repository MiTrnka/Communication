using APIIdentity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIIdentity.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Vyžaduje autentizaci pro všechny endpointy
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    [Authorize(Policy = "RequireAdminRole")] // Pouze pro adminy
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userManager.Users
            .Select(u => new {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.Created
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Created,
            Roles = roles
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateProfileModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        // V produkci přidat validaci vstupů
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            return Ok(new { message = "Profile updated successfully" });
        }

        return BadRequest(new { errors = result.Errors });
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.ChangePasswordAsync(
            user, model.CurrentPassword, model.NewPassword);

        if (result.Succeeded)
        {
            // V produkci přidat:
            // - Odeslání notifikačního emailu
            // - Logování změny
            // - Vynucení nového přihlášení

            return Ok(new { message = "Password changed successfully" });
        }

        return BadRequest(new { errors = result.Errors });
    }
}