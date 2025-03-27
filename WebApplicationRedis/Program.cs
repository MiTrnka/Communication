//Po nakop�rov�n� Redis do c:\Redis se zaregistruje redis jako windows slu�ba pomoc�:
//   redis-server --service-install redis.windows.conf --loglevel verbose
//a spust� se:
//   redis-server --service-start, p��padn� zastaven� redis-server --service-stop a odinstalov�n� slu�by redis-server --service-uninstall
//spu�t�n� redis klienta: j�t v  cmd do c:\Redis a spustit redis-cli.exe
// p��kazi v redis-cli:
//      set klic hodnota    - ulo�� hodnotu do klice
//      get klic            - z�sk� hodnotu z klice
//      keys *              - zobraz� v�echny kl��e
//      HGETALL "SessionKl��" � zobraz� ulo�en� hodnoty z ulo�en� session
//      type klic           - zjist� typ kl��e (string, list...)
//      LPUSH klic "polozka1", "polozka2" - vlo�� polo�ky na za��tek listu, RPUSH by vkl�dal zprava
//      del klic            - sma�e kl��
//      flushall            - sma�e v�echny kl��e
//      exists klic         - zjist�, zda kl�� existuje
//      expire klic cas     - nastav� �as vypr�en� kl��e v sekund�ch
//      ttl klic            - zjist�, kolik zb�v� �asu do vypr�en� kl��e
//      exit                - ukon�� redis-cli, ale redis b�� d�l

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
        // Pokud by nebyl Redis viz n�e dostupn�, pou�ije se tento cache, kter� je pouze v RAM
        builder.Services.AddDistributedMemoryCache();

        // P�id�n� Distributed Redis Cache. Pokud by zde nastala chyba, vyu�ije se DistributedMemoryCache viz v��e
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
