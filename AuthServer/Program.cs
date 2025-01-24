//NuGet Microsoft.AspNetCore.Authentication.JwtBearer
//NuGet System.IdentityModel.Tokens.Jwt
//NuGet Swashbuckle.AspNetCore

/*
Implementace OAuth 2.0 serveru je navr�ena jako minimalistick� v�ukov� model demonstruj�c� z�kladn� principy autentizace a autorizace:
Kl��ov� komponenty:

TokenService: Centr�ln� slu�ba pro generov�n� a validaci token�
In-memory �lo�i�t� refresh token� pomoc� ConcurrentDictionary
Generov�n� JWT access token� s kr�tkodobou platnost� (15 minut)
Refresh tokeny s dlouhodobou platnost� (7 dn�)
Pou�it� symetrick�ho �ifrov�n� s pevn�m secret key

Hlavn� charakteristiky:

Zjednodu�en� model bez kompletn� implementace OAuth 2.0
Demonstrace principu odd�len� refresh a access token�
Uk�zka pou�it� JWT pro generov�n� p��stupov�ch token�
Minim�ln� bezpe�nostn� opat�en� pro v�ukov� ��ely 
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