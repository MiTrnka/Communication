/* Aplikace um� na endpointu /token vygenerovat bearer token ve form�tu JWT
N�sledn� ten token validuje (vlo�en v http jako hlavi�ka Authorization s hodnotou Bearer mezera a pak ten token)*/

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
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "SuperTajnyKlicProVyvojODelceAlespon32Bytu"; // Z�sk�n� tajn�ho kl��e z konfigurace nebo default
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "http://localhost:5041"; // Z�sk�n� issuer z konfigurace nebo default
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "http://localhost:5041"; // Z�sk�n� audience z konfigurace nebo default
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)) // Nastavuje tajn� kl�� pro ov��en� podpisu
        };
    });

// Nastaven� autorizace (nutn� pro [Authorize])
builder.Services.AddAuthorization();

// P�id�n� slu�eb pro kontrolery
builder.Services.AddControllers();

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

// Mapov�n� kontroler�
app.MapControllers();

app.MapGet("/tokenAdmin", [AllowAnonymous] () =>
{
    // Generov�n� JWT tokenu
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(jwtSecret);
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        //Pole mnou definovan�ch atribut�, kter� chci dostat do tokenu a pak je moci p�i autorizaci n�jak vyu��t
        Subject = new ClaimsIdentity(new[] {
            new Claim("id", "123" /*Zde by m�sto n�hodn�ho ��sla bylo nap��klad Id u�ivatele z datab�ze*/),
            //new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "Admin") // Mus�m p�ed role dopsat http://schemas.microsoft.com/ws/2008/06/identity/claims/, proto�e to je defaultn� namespace pro role, dal�� jsou nap��klad name, emailaddress...
            new Claim("mujclaimproroli", "Admin") // Nov� claim pro roli
        }), // P�id�n� custom claimy
        Expires = DateTime.UtcNow.AddMinutes(jwtExpireMinutes),    // Nastaven� expirace tokenu
        Issuer = jwtIssuer,               // Nastaven� url vydavatele
        Audience = jwtAudience,            // Nastaven� url konzumenta
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature) // Podpis tokenu
    };
    var token = tokenHandler.CreateToken(tokenDescriptor);
    var tokenString = tokenHandler.WriteToken(token);

    return Results.Ok(new { Token = tokenString }); // Vr�cen� tokenu v JSON form�tu
})
.WithOpenApi(); // Generov�n� OpenAPI pro /token

// Endpoint chr�n�n� tokenem
app.MapGet("/datetime", [Authorize] (HttpContext context) =>
{
    var claims = context.User.Claims;

    // Z�sk�n� claimu pro roli
    var roleClaim = claims.FirstOrDefault(c => c.Type == "mujclaimproroli");

    // Kontrola, zda v tokenu je mujclaimproroli nastavena na "Admin"
    if (roleClaim != null && roleClaim.Value == "Admin")
    {
        // Pokud se sem k�d dostane, tak m� u�ivatel validn� token v�etn� claimu s rol� Admin
        return Results.Ok(new { DateTime = DateTime.Now });
    }
    // Pokud role nen� admin, tak vr�t�me status code 403
    return Results.Forbid();
})
.WithOpenApi(); // Generov�n� OpenAPI pro /datetime

// Spu�t�n� aplikace
app.Run();
