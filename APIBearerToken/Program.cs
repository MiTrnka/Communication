/* Aplikace umí na endpointu /tokenAdmin vygenerovat bearer token ve formátu JWT
Následnì ten token validuje (vložen v http jako hlavièka Authorization s hodnotou Bearer mezera a pak ten token)*/

//NuGet package: Microsoft.AspNetCore.Authentication.JwtBearer
//NuGet package: System.IdentityModel.Tokens.Jwt
// NuGet Swashbuckle.AspNetCore - pro OpenAPI (Swagger)
// NuGet Microsoft.AspNetCore.OpenApi

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Konfigurace JWT
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "SuperTajnyKlicProVyvojODelceAlespon32Bytu"; // Získání tajného klíèe
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "http://localhost:5041"; // Získání issuer
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "http://localhost:5041"; // Získání audience
var jwtExpireMinutes = 10; // Nastavení expirace tokenu na 10 minut, menší hodnoty expirovaly stejnì pozdìji

// Nastavení, že se má používat JWT Bearer authentication jako výchozí autentizaèní mechanismus v celé naší aplikaci a to tam, kde je [Authorize]
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,           // Ovìøuje, jestli se v tokenu shoduje issuer (vydavatel)
            ValidateAudience = true,         // Ovìøuje, jestli se v tokenu shoduje audience (pøíjemce)
            ValidateLifetime = true,        // Ovìøuje expiraci tokenu
            ValidateIssuerSigningKey = true, // Ovìøuje podpis tokenu
            ValidIssuer = jwtIssuer,        // Nastavení validního issuer
            ValidAudience = jwtAudience,     // Nastavení validní audience
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)) // Nastavuje tajný klíè pro ovìøení podpisu (SymmetricSecurityKey si z tokenu zjistí algoritmus, jakým byl podpis vytvoøen)
        };
    });

// Nastavení autorizace (nutné pro [Authorize])
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(ClaimTypes.Role, "Admin");
    });

    options.AddPolicy("UserOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(ClaimTypes.Role, "User");
    });

});

// Pøidání služeb pro kontrolery
//builder.Services.AddControllers();

// Registrace OpenAPI (Swagger)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Sestavení aplikace
var app = builder.Build();

// Použití OpenAPI (Swagger)
app.UseSwagger();
app.UseSwaggerUI();

// Použití autentizace a autorizace
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/tokenAdmin", [AllowAnonymous] () =>
{
    // Generování JWT tokenu
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Role, "Admin"), // Oficiální claim pro roli
        new Claim("mujClaim", "nic") // Nový vlastní claim pro roli
    };
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Issuer = jwtIssuer,
        Audience = jwtAudience,
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddMinutes(jwtExpireMinutes),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)), SecurityAlgorithms.HmacSha256)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
});
//.WithOpenApi(); // Generování OpenAPI pro /token, jelikož jsem dal app.UseSwagger();, tak toto mohu zakomentovat

app.MapGet("/tokenuser", [AllowAnonymous] () =>
{
    // Generování JWT tokenu
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Role, "User"), // Oficiální claim pro roli
        new Claim("mujClaim", "mujClaimHodnota") // Nový vlastní claim pro roli
    };
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Issuer = jwtIssuer,
        Audience = jwtAudience,
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddMinutes(jwtExpireMinutes),
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)), SecurityAlgorithms.HmacSha256)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
});
//.WithOpenApi(); // Generování OpenAPI pro /token, jelikož jsem dal app.UseSwagger();, tak toto mohu zakomentovat

// Endpoint chránìný tokenem a ète hodnotu claimu "mujclaimproroli"
app.MapGet("/datetime1", [Authorize] (HttpContext context) =>
{
    var claims = context.User.Claims;

    // Získání claimu pro roli
    var roleClaim = claims.FirstOrDefault(c => c.Type == "mujClaim");

    // Kontrola, zda v tokenu je mujclaimproroli nastavena na "Admin"
    if (roleClaim != null && roleClaim.Value == "mujClaimHodnota")
    {
        // Pokud se sem kód dostane, tak má uživatel validní token vèetnì claimu s rolí Admin
        return Results.Ok(new { DateTime = DateTime.Now });
    }
    // Pokud role není admin, tak vrátíme status code 403
    return Results.Forbid();
});
//.WithOpenApi(); // Generování OpenAPI pro /token, jelikož jsem dal app.UseSwagger();, tak toto mohu zakomentovat

// Endpoint chránìný tokenem plus moje politika AdminOnly
app.MapGet("/datetime2", [Authorize(Policy = "AdminOnly")] (HttpContext context) =>
{
    return Results.Ok(new { DateTime = DateTime.Now });
});
//.WithOpenApi(); // Generování OpenAPI pro /token, jelikož jsem dal app.UseSwagger();, tak toto mohu zakomentovat

// Vypíše, zda je uživatel autentizován (pokud nedodám platný JWT token, tak vypíše false)
app.MapGet("/identita", (HttpContext context) =>
{
    return Results.Ok(context.User.Identity.IsAuthenticated.ToString());
});
//.WithOpenApi(); // Generování OpenAPI pro /token, jelikož jsem dal app.UseSwagger();, tak toto mohu zakomentovat

// Spuštìní aplikace
app.Run();
