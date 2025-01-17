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

        // Pøidání Distributed Memory Cache (jako fallback)
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
