// NuGet Microsoft.AspNetCore.Identity.EntityFrameworkCore
// NuGet Microsoft.AspNetCore.Authentication.Google
// NuGet Microsoft.AspNetCore.Authentication.JwtBearer
// NuGet Npgsql.EntityFrameworkCore.PostgreSQL
// Microsoft.EntityFrameworkCore.Design
// NuGet Microsoft.EntityFrameworkCore
// NuGet Microsoft.EntityFrameworkCore.Relational
// NuGet Google.Apis.Auth

using APIASPNETCoreIdentity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Konfigurace databáze
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Konfigurace Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// JWT konfigurace
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Konfigurace JWT Bearer
})
.AddGoogle(options =>
{
    // Konfigurace Google OAuth
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
});

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.Run();