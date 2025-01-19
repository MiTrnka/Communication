/* Aplikace umí na endpointu /token vygenerovat bearer token ve formátu JWT
Následnì ten token validuje (vložen v http jako hlavièka Authorization s hodnotou Bearer mezera a pak ten token)*/

//NuGet package: Microsoft.AspNetCore.Authentication.JwtBearer
//NuGet package: System.IdentityModel.Tokens.Jwt
// NuGet Swashbuckle.AspNetCore - pro OpenAPI (Swagger)
// NuGet Microsoft.AspNetCore.OpenApi

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Konfigurace JWT
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "SuperTajnyKlicProVyvojODelceAlespon32Bytu"; // Získání tajného klíèe z konfigurace nebo default
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "http://localhost:5041"; // Získání issuer z konfigurace nebo default
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "http://localhost:5041"; // Získání audience z konfigurace nebo default
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)) // Nastavuje tajný klíè pro ovìøení podpisu
        };
    });

// Nastavení autorizace (nutné pro [Authorize])
builder.Services.AddAuthorization();

// Pøidání služeb pro kontrolery
builder.Services.AddControllers();

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

// Mapování kontrolerù
app.MapControllers();

app.MapGet("/tokenAdmin", [AllowAnonymous] () =>
{
    // Generování JWT tokenu
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(jwtSecret);
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        //Pole mnou definovaných atributù, které chci dostat do tokenu a pak je moci pøi autorizaci nìjak využít
        Subject = new ClaimsIdentity(new[] {
            new Claim("id", "123" /*Zde by místo náhodného èísla bylo napøíklad Id uživatele z databáze*/),
            //new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "Admin") // Musím pøed role dopsat http://schemas.microsoft.com/ws/2008/06/identity/claims/, protože to je defaultní namespace pro role, další jsou napøíklad name, emailaddress...
            new Claim("mujclaimproroli", "Admin") // Nový claim pro roli
        }), // Pøidání custom claimy
        Expires = DateTime.UtcNow.AddMinutes(jwtExpireMinutes),    // Nastavení expirace tokenu
        Issuer = jwtIssuer,               // Nastavení url vydavatele
        Audience = jwtAudience,            // Nastavení url konzumenta
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature) // Podpis tokenu
    };
    var token = tokenHandler.CreateToken(tokenDescriptor);
    var tokenString = tokenHandler.WriteToken(token);

    return Results.Ok(new { Token = tokenString }); // Vrácení tokenu v JSON formátu
})
.WithOpenApi(); // Generování OpenAPI pro /token

// Endpoint chránìný tokenem
app.MapGet("/datetime", [Authorize] (HttpContext context) =>
{
    var claims = context.User.Claims;

    // Získání claimu pro roli
    var roleClaim = claims.FirstOrDefault(c => c.Type == "mujclaimproroli");

    // Kontrola, zda v tokenu je mujclaimproroli nastavena na "Admin"
    if (roleClaim != null && roleClaim.Value == "Admin")
    {
        // Pokud se sem kód dostane, tak má uživatel validní token vèetnì claimu s rolí Admin
        return Results.Ok(new { DateTime = DateTime.Now });
    }
    // Pokud role není admin, tak vrátíme status code 403
    return Results.Forbid();
})
.WithOpenApi(); // Generování OpenAPI pro /datetime

// Spuštìní aplikace
app.Run();
