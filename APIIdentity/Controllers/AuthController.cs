//Volně přístupný controller pro registraci a přihlášení uživatele s JWT autentizací. V produkci je nutné přidat validaci vstupů, odesílání potvrzovacího emailu, logování událostí a další bezpečnostní opatření.
using APIIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace APIIdentity.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterModel model)
    {
        // V produkci přidat validaci složitosti hesla a dalších údajů
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            // Přidání výchozí role "User"
            await _userManager.AddToRoleAsync(user, "User");

            // V produkci přidat:
            // - Odeslání potvrzovacího emailu
            // - Logování události
            // - Notifikace admina

            return Ok(new { message = "Registration successful" });
        }

        return BadRequest(new { errors = result.Errors });
    }

    //Zkontroluje zadaný email a heslo a vytvoří JWT token
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            return Unauthorized();
        }

        //Metoda ověří heslo a provede přihlášení (false zde znamená, že se nezamkne účet při špatném hesle)
        var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
        if (result.Succeeded)
        {
            var token = await GenerateJwtToken(user);

            // V produkci přidat:
            // - Aktualizace LastLoginDate
            // - Logování přihlášení
            // - Kontrola suspicious aktivit

            return Ok(new { token });
        }

        return Unauthorized();
    }

    private async Task<string> GenerateJwtToken(ApplicationUser user)
    {
        var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, user.Email),

                //Používá se pro sledování a případné zneplatnění konkrétních tokenů, momentálně není využito
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

                // Obsahuje ID uživatele z databáze, momentálně není využito
                new Claim(JwtRegisteredClaimNames.NameId, user.Id)
            };

        // Přidání rolí do claims
        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddDays(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}