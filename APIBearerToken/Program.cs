/* Aplikace um� na endpointu /tokenAdmin vygenerovat bearer token ve form�tu JWT
N�sledn� ten token validuje (vlo�en v http jako hlavi�ka Authorization s hodnotou Bearer mezera a pak ten token)*/

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
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "SuperTajnyKlicProVyvojODelceAlespon32Bytu"; // Z�sk�n� tajn�ho kl��e
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "http://localhost:5041"; // Z�sk�n� issuer
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "http://localhost:5041"; // Z�sk�n� audience
var jwtExpireMinutes = 10; // Nastaven� expirace tokenu na 10 minut, men�� hodnoty expirovaly stejn� pozd�ji

// Nastaven�, �e se m� pou��vat JWT Bearer authentication jako v�choz� autentiza�n� mechanismus v cel� na�� aplikaci a to tam, kde je [Authorize]
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,           // Ov��uje, jestli se v tokenu shoduje issuer (vydavatel)
            ValidateAudience = true,         // Ov��uje, jestli se v tokenu shoduje audience (p��jemce)
            ValidateLifetime = true,        // Ov��uje expiraci tokenu
            ValidateIssuerSigningKey = true, // Ov��uje podpis tokenu
            ValidIssuer = jwtIssuer,        // Nastaven� validn�ho issuer
            ValidAudience = jwtAudience,     // Nastaven� validn� audience
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)) // Nastavuje tajn� kl�� pro ov��en� podpisu (SymmetricSecurityKey si z tokenu zjist� algoritmus, jak�m byl podpis vytvo�en)
        };
    });

// Nastaven� autorizace (nutn� pro [Authorize])
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

// P�id�n� slu�eb pro kontrolery
//builder.Services.AddControllers();

// Registrace OpenAPI (Swagger)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Sestaven� aplikace
var app = builder.Build();

// Pou�it� OpenAPI (Swagger)
app.UseSwagger();
app.UseSwaggerUI();

// Pou�it� autentizace a autorizace
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/tokenAdmin", [AllowAnonymous] () =>
{
    // Generov�n� JWT tokenu
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Role, "Admin"), // Ofici�ln� claim pro roli
        new Claim("mujClaim", "nic") // Nov� vlastn� claim pro roli
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
//.WithOpenApi(); // Generov�n� OpenAPI pro /token, jeliko� jsem dal app.UseSwagger();, tak toto mohu zakomentovat

app.MapGet("/tokenuser", [AllowAnonymous] () =>
{
    // Generov�n� JWT tokenu
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Role, "User"), // Ofici�ln� claim pro roli
        new Claim("mujClaim", "mujClaimHodnota") // Nov� vlastn� claim pro roli
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
//.WithOpenApi(); // Generov�n� OpenAPI pro /token, jeliko� jsem dal app.UseSwagger();, tak toto mohu zakomentovat

// Endpoint chr�n�n� tokenem a �te hodnotu claimu "mujclaimproroli"
app.MapGet("/datetime1", [Authorize] (HttpContext context) =>
{
    var claims = context.User.Claims;

    // Z�sk�n� claimu pro roli
    var roleClaim = claims.FirstOrDefault(c => c.Type == "mujClaim");

    // Kontrola, zda v tokenu je mujclaimproroli nastavena na "Admin"
    if (roleClaim != null && roleClaim.Value == "mujClaimHodnota")
    {
        // Pokud se sem k�d dostane, tak m� u�ivatel validn� token v�etn� claimu s rol� Admin
        return Results.Ok(new { DateTime = DateTime.Now });
    }
    // Pokud role nen� admin, tak vr�t�me status code 403
    return Results.Forbid();
});
//.WithOpenApi(); // Generov�n� OpenAPI pro /token, jeliko� jsem dal app.UseSwagger();, tak toto mohu zakomentovat

// Endpoint chr�n�n� tokenem plus moje politika AdminOnly
app.MapGet("/datetime2", [Authorize(Policy = "AdminOnly")] (HttpContext context) =>
{
    return Results.Ok(new { DateTime = DateTime.Now });
});
//.WithOpenApi(); // Generov�n� OpenAPI pro /token, jeliko� jsem dal app.UseSwagger();, tak toto mohu zakomentovat

// Vyp�e, zda je u�ivatel autentizov�n (pokud nedod�m platn� JWT token, tak vyp�e false)
app.MapGet("/identita", (HttpContext context) =>
{
    return Results.Ok(context.User.Identity.IsAuthenticated.ToString());
});
//.WithOpenApi(); // Generov�n� OpenAPI pro /token, jeliko� jsem dal app.UseSwagger();, tak toto mohu zakomentovat

// Spu�t�n� aplikace
app.Run();
