//Po nakop�rov�n� Redis do c:\Redis se zaregistruje redis jako windows slu�ba pomoc� redis-server --service-install redis.windows.conf --loglevel verbose
//a spust� se redis-server --service-start, p��padn� zastaven� redis-server --service-stop a odinstalov�n� slu�by redis-server --service-uninstall
//spu�t�n� redis klienta redis-cli.exe
//NuGet bal��ek: Microsoft.Extensions.Caching.StackExchangeRedis
//V appsettings p�idat ConnectionString, upravit program.cs a str�nky, kde chci session pou��vat
namespace WebApplicationRedis;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();





        // P�idejte IHttpContextAccessor
        builder.Services.AddHttpContextAccessor();

        // P�id�n� Distributed Memory Cache (jako fallback, tedy z�lo�n� �e�en�)
        builder.Services.AddDistributedMemoryCache();

        // P�id�n� Distributed Redis Cache
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = builder.Configuration.GetConnectionString("Redis");
            options.InstanceName = "PrefixProNazevKlicekvuliKoliziMeziAPlikacema"; // Voliteln�: Prefixt pro kl��e v Redisu
        });

        // P�id�n� podpory pro Session
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30); // Doba neaktivity p�ed vypr�en�m session
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.Name = ".NazevSessionCookie"; // Zde definujete n�zev cookie pro session, te�ka ��k�, �e cookie se bude odes�lat i pro subdomeny
        });





        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }
        app.UseStaticFiles();

        app.UseRouting();

        // Pou�it� Session middleware(MUS� B�T PO UseRouting())
        app.UseSession();

        app.UseAuthorization();

        app.MapRazorPages();

        app.Run();
    }
}
