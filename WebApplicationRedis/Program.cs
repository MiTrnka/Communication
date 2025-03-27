//Po nakopírování Redis do c:\Redis se zaregistruje redis jako windows služba pomocí:
//   redis-server --service-install redis.windows.conf --loglevel verbose
//a spustí se:
//   redis-server --service-start, pøípadné zastavení redis-server --service-stop a odinstalování služby redis-server --service-uninstall
//spuštìní redis klienta: jít v  cmd do c:\Redis a spustit redis-cli.exe
// pøíkazi v redis-cli:
//      set klic hodnota    - uloží hodnotu do klice
//      get klic            - získá hodnotu z klice
//      keys *              - zobrazí všechny klíèe
//      HGETALL "SessionKlíè" – zobrazí uložené hodnoty z uložené session
//      type klic           - zjistí typ klíèe (string, list...)
//      LPUSH klic "polozka1", "polozka2" - vloží položky na zaèátek listu, RPUSH by vkládal zprava
//      del klic            - smaže klíè
//      flushall            - smaže všechny klíèe
//      exists klic         - zjistí, zda klíè existuje
//      expire klic cas     - nastaví èas vypršení klíèe v sekundách
//      ttl klic            - zjistí, kolik zbývá èasu do vypršení klíèe
//      exit                - ukonèí redis-cli, ale redis bìží dál

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
        // Pokud by nebyl Redis viz níže dostupný, použije se tento cache, který je pouze v RAM
        builder.Services.AddDistributedMemoryCache();

        // Pøidání Distributed Redis Cache. Pokud by zde nastala chyba, využije se DistributedMemoryCache viz výše
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
