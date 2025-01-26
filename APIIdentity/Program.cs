// NuGet Microsoft.AspNetCore.Identity.EntityFrameworkCore
// NuGet Microsoft.EntityFrameworkCore.Tools
// NuGet Npgsql.EntityFrameworkCore.PostgreSQL
// NuGet Microsoft.AspNetCore.Authentication.JwtBearer
// NuGet Swashbuckle.AspNetCore

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using APIIdentity.Data;
using APIIdentity.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;


var builder = WebApplication.CreateBuilder(args);

// Konfigurace pøipojení k PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Konfigurace Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    // Nastavení požadavkù na heslo - pro produkci zvážit pøísnìjší pravidla
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 1;

    // Nastavení uzamèení úètu - pro produkci upravit podle potøeb
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Konfigurace JWT autentizace
builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        NameClaimType = ClaimTypes.Email,
        RoleClaimType = ClaimTypes.Role
    };
});

builder.Services.AddAuthorization(options => {
    // Definice politik - v produkci rozšíøit podle potøeb
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy("RequireModeratorRole", policy =>
        policy.RequireRole("Moderator"));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// Inicializace výchozích rolí a admin úètu pøi startu aplikace
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    await SeedData.Initialize(roleManager, userManager);
}

app.Run();