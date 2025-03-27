//NuGet Microsoft.Extensions.Hosting
namespace Console_IoCDI;
using Microsoft.Extensions.DependencyInjection;

public class Notifikace
{
    public Guid Id { get; } = Guid.NewGuid();
    public void VypisGuid()
    {
        Console.WriteLine(Id.ToString());
    }
}

public class Aplikace (Notifikace notifikace)
{
    public void Vypis()
    {
        notifikace.VypisGuid();
    }
}

public class Program
{
    static void Main(string[] args)
    {
         //Vypíše v obou případech stejný Guid
        /*ServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<Aplikace>();
        serviceCollection.AddTransient<Notifikace>();
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        var aplikace1 = serviceProvider.GetService<Aplikace>();
        var aplikace2 = serviceProvider.GetService<Aplikace>();
        aplikace1.Vypis();
        aplikace2.Vypis();*/
        

        //Vypíše již jiná Guid, protože služba Aplikace je vytvořena pro každý scope jiná
        /*ServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<Aplikace>();
        serviceCollection.AddTransient<Notifikace>();
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        var scope1 = serviceProvider.CreateScope();
        var aplikace1 = scope1.ServiceProvider.GetService<Aplikace>();
        var scope2 = serviceProvider.CreateScope();
        var aplikace2 = scope2.ServiceProvider.GetService<Aplikace>();
        aplikace1.Vypis();
        aplikace2.Vypis();*/

        //Vypíše Guid stejná, i když Aplikace zaregistrovaná jako Scoped je vytvořena pro každý scope jiná, ale interně používá Notifikaci, která je singleton
        ServiceCollection serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<Aplikace>();
        serviceCollection.AddSingleton<Notifikace>();
        //serviceCollection.AddSingleton<Notifikace>(sp=>new Notifikace());//Pokud by notifikace měla více konstruktorů a já bych chtěl pomocí Func<IServiceProvider,Notifikace> specifikovat, jak se vytvoří
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        var scope1 = serviceProvider.CreateScope();
        var aplikace1 = scope1.ServiceProvider.GetService<Aplikace>();//při nenalezení služby by se vrátil null
        //var aplikace1 = scope1.ServiceProvider.GetRequiredService<Aplikace>();//při nenalezení služby by se vyhodila výjimka
        var scope2 = serviceProvider.CreateScope();
        var aplikace2 = scope2.ServiceProvider.GetService<Aplikace>();
        aplikace1.Vypis();
        aplikace2.Vypis();


    }
}

