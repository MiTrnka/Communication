// NuGet StackExchange.Redis
namespace ConsoleAppRedis;

using System;
using StackExchange.Redis;

internal class Program
{
    static void Main(string[] args)
    {
        // Připojení k lokálnímu Redis serveru
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost"); // automaticky doplní port 6379, ale mohl bych zadat za dvojtečku jiný port (případně i server)

        // Získání databáze s číslem 0, jiné číslo než 0 bych pak musel uvést jako parametr
        // Je to pro logické oddělení dat
        IDatabase db = redis.GetDatabase();

        // Zápis hodnoty do Redis
        string key = "user:example";
        string value = "Jan Novák";
        db.StringSet(key, value);
        Console.WriteLine($"Zapsáno do Redis: {key} = {value}");

        // Čtení hodnoty z Redis
        string readValue = db.StringGet(key);
        Console.WriteLine($"Přečteno z Redis: {key} = {readValue}");

        // Příklad práce s hashem
        HashEntry[] hashEntries = new HashEntry[]
        {
            new HashEntry("name", "Petr"),
            new HashEntry("age", "35"),
            new HashEntry("city", "Praha")
        };

        string hashKey = "user:1";
        db.HashSet(hashKey, hashEntries);
        Console.WriteLine("Uložen hash do Redis");

        // Čtení všech polí hashe
        HashEntry[] retrievedEntries = db.HashGetAll(hashKey);
        Console.WriteLine("Načtení hashe:");
        foreach (var entry in retrievedEntries)
        {
            Console.WriteLine($"{entry.Name}: {entry.Value}");
        }
    }
}
