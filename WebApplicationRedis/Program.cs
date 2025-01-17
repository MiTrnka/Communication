//Po nakopírování Redis do c:\Redis se zaregistruje redis jako windows služba pomocí redis-server --service-install redis.windows.conf --loglevel verbose
//a spustí se redis-server --service-start, pøípadné zastavení redis-server --service-stop a odinstalování služby redis-server --service-uninstall
//spuštìní redis klienta redis-cli.exe
//NuGet balíèek: Microsoft.Extensions.Caching.StackExchangeRedis
//V appsettings pøidat ConnectionString, upravit program.cs a stránky, kde chci session používat
namespace WebApplicationRedis;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();





        // Pøidejte IHttpContextAccessor
        builder.Services.AddHttpContextAccessor();

        // Pøidání Distributed Memory Cache (jako fallback, tedy záložní øešení)
        builder.Services.AddDistributedMemoryCache();

        // Pøidání Distributed Redis Cache
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetConnectionString("Redis");
            options.InstanceName = "PrefixProNazevKlicekvuliKoliziMeziAPlikacema"; // Volitelné: Prefixt pro klíèe v Redisu
        });

        // Pøidání podpory pro Session
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30); // Doba neaktivity pøed vypršením session
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.Name = ".NazevSessionCookie"; // Zde definujete název cookie pro session, teèka øíká, že cookie se bude odesílat i pro subdomeny
        });





        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }
        app.UseStaticFiles();

        app.UseRouting();

        // Použití Session middleware(MUSÍ BÝT PO UseRouting())
        app.UseSession();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}
