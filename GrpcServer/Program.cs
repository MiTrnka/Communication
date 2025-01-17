/*
Serverová část
Projekt založen jako: ASP.NET Core gRPC Service
.proto build action: Protobuf compiler, gRPC Stubb classes: Server only

Serverová část nabízí 3 služby (pro každou je proto soubor s popisem jejich metod a tříd pro ně použitých jako vstupni a navratové parametry)

Služba CumulativeLog:
V souboru cumulativelog.proto mám definici služby CumulativeLogService, jejích 2 metod,
s jejich vstupních a výstupních parametrù. V build procesu se z ní vygeneruje abstraktní
třída CumulativeLog.CumulativeLogBase, která má 2 abstraktní metody (popis v .proto), 
které v souboru CumulativeLogService.cs implementuji v jejím potomkovi. 
Tak nadefinuji, co ta služba (její metody) bude dělat.

Defaultní nastavení je takové, že pro každé volání metody nějaké služby se na serveru 
vytvoří nová instance této služby (CumulativeLogService), proto mám tu kumulativní
proměnnou statickou (cumulativeText)

*/

using gRPCServer.Services;

namespace GrpcServer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddGrpc();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.MapGrpcService<GreeterService>();
        app.MapGrpcService<CumulativeLogService>();
        app.MapGrpcService<StreamingServiceImpl>();

        app.MapGet("/", () => "");

        app.Run();
    }
}