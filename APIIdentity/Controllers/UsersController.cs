// Controller vyžadující autentizaci (zda je token ok a neexpirovaný) a autorizaci, poskytuje endpointy pro získání seznamu uživatelů, získání profilu uživatele, aktualizaci profilu a změnu hesla
using APIIdentity.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
        //Získání emailu z přihlášeného uživatele, User je vlastnost z ControllerBase
        //obsahuje informace o přihlášeném uživateli extrahované z tokenu. ASP.NET Core ji automaticky naplní z Authorization hlavičky (místo tokenu by to mohlo být třeba autorizační cookie...)
        var email = User.FindFirstValue(ClaimTypes.Email);
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null) return NotFound();

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
        //Získání emailu z přihlášeného uživatele, User je vlastnost z ControllerBase
        //obsahuje informace o přihlášeném uživateli extrahované z tokenu. ASP.NET Core ji automaticky naplní z Authorization hlavičky (místo tokenu by to mohlo být třeba autorizační cookie...)
        var email = User.FindFirstValue(ClaimTypes.Email);
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null) return NotFound();

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
        var email = User.FindFirstValue(ClaimTypes.Email);
        var user = await _userManager.FindByEmailAsync(email);

        if (user == null) return NotFound();

        var result = await _userManager.ChangePasswordAsync(
            user,
            model.CurrentPassword,
            model.NewPassword
        );

        if (result.Succeeded)
        {
            return Ok(new { message = "Password changed successfully" });
        }

        return BadRequest(new { errors = result.Errors });
    }
}