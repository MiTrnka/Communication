namespace APIASPNETCoreIdentity;

using Google.Apis.Auth;

// Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class ApplicationUser : IdentityUser
{
    // Můžete přidat další vlastní pole pro uživatele
    public string DisplayName { get; set; }
}

// Data/ApplicationDbContext.cs
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Zde můžete přidat vlastní konfiguraci modelů
    }
}

// Controllers/AuthController.cs

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

    // Lokální přihlášení
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null) return Unauthorized();

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName, model.Password, false, false);

        if (result.Succeeded)
        {
            return Ok(new
            {
                token = GenerateJwtToken(user),
                email = user.Email
            });
        }

        return Unauthorized();
    }

    // Google přihlášení
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginModel model)
    {
        var payload = await VerifyGoogleToken(model.IdToken);
        if (payload == null) return Unauthorized();

        var user = await _userManager.FindByEmailAsync(payload.Email);
        if (user == null)
        {
            // Vytvoří nového uživatele, pokud neexistuje
            user = new ApplicationUser
            {
                Email = payload.Email,
                UserName = payload.Email
            };
            await _userManager.CreateAsync(user);
        }

        return Ok(new
        {
            token = GenerateJwtToken(user),
            email = user.Email
        });
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<GoogleJsonWebSignature.Payload> VerifyGoogleToken(string idToken)
    {
        // Implementace ověření Google tokenu
        // Použijte GoogleJsonWebSignature.ValidateAsync()
        return null; // Placeholder
    }
}

// Models/LoginModels.cs
public class LoginModel
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class GoogleLoginModel
{
    public string IdToken { get; set; }
}