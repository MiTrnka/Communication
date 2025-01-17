/*
ASP.NET Core Empty
NuGet balíček: Microsoft.AspNetCore.SignalR

SignalR umožňuje snadné vytváření real-time komunikace u webových aplikací. Využívá tzv. 
"Hubs" pro správu komunikace mezi klienty a serverem. Hub je třída na serveru, 
která slouží jako hlavní bod komunikace. Klienti se připojují k tomuto Hubu a 
mohou posílat zprávy, které Hub zpracovává.

AddSignalR(): Tato metoda registruje služby SignalR potřebné pro fungování Hubs.

MapHub<ChatHub>("/chatHub"): Tato metoda mapuje ChatHub na určitou URL, 
v tomto případě /chatHub. Klienti použijí tuto URL pro připojení k Hubu. */
namespace SignalRServer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Přidání SignalR
        builder.Services.AddSignalR();

        var app = builder.Build();

        // Mapování ChatHub
        app.MapHub<SignalRServer.ChatHub>("/chatHub");

        app.MapGet("/", () => "Server pro chat spusten.");

        app.Run();
    }
}
