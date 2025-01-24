//NuGet Microsoft.AspNetCore.Authentication.JwtBearer
//NuGet System.IdentityModel.Tokens.Jwt
//NuGet Swashbuckle.AspNetCore

/*
Implementace OAuth 2.0 serveru je navržena jako minimalistický výukový model demonstrující základní principy autentizace a autorizace:
Klíèové komponenty:

TokenService: Centrální služba pro generování a validaci tokenù
In-memory úložištì refresh tokenù pomocí ConcurrentDictionary
Generování JWT access tokenù s krátkodobou platností (15 minut)
Refresh tokeny s dlouhodobou platností (7 dní)
Použití symetrického šifrování s pevným secret key

Hlavní charakteristiky:

Zjednodušený model bez kompletní implementace OAuth 2.0
Demonstrace principu oddìlení refresh a access tokenù
Ukázka použití JWT pro generování pøístupových tokenù
Minimální bezpeènostní opatøení pro výukové úèely 
*/

using System.Text;
using AuthServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Dependency Injection
builder.Services.AddSingleton<TokenService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "OAuth Demo Server", Version = "v1" });
});

// JWT Authentication
var secretKey = Encoding.UTF8.GetBytes("SuperSecretKeyThatShouldBeInConfigInRealLife");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(secretKey),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();