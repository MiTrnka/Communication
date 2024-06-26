/*
ASP.NET Core Empty
NuGet bal��ek: Microsoft.AspNetCore.SignalR

SignalR umo��uje snadn� vytv��en� real-time webov�ch aplikac�. Vyu��v� tzv. 
"Hubs" pro spr�vu komunikace mezi klienty a serverem. Hub je t��da na serveru, 
kter� slou�� jako hlavn� bod komunikace. Klienti se p�ipojuj� k tomuto Hubu a 
mohou pos�lat zpr�vy, kter� Hub zpracov�v�.

AddSignalR(): Tato metoda registruje slu�by SignalR pot�ebn� pro fungov�n� Hubs.

MapHub<ChatHub>("/chatHub"): Tato metoda mapuje ChatHub na ur�itou URL, 
v tomto p��pad� /chatHub. Klienti pou�ij� tuto URL pro p�ipojen� k Hubu. */
namespace SignalRServer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // P�id�n� SignalR
        builder.Services.AddSignalR();

        var app = builder.Build();

        // Mapov�n� ChatHub
        app.MapHub<SignalRServer.ChatHub>("/chatHub");

        app.MapGet("/", () => "Server pro chat spusten.");

        app.Run();
    }
}
