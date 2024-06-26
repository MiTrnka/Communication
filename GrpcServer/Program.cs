/*
Serverov� ��st
Projekt zalo�en jako: ASP.NET Core gRPC Service
.proto build action: Protobuf compiler, gRPC Stubb classes: Server only

Serverov� ��st nab�z� 3 slu�by (pro ka�dou je proto soubor s popisem jejich metod a t��d pro ne pouzitych jako vstupni a navratov� parametry)

Slu�ba CumulativeLog:
V souboru cumulativelog.proto m�m definici slu�by CumulativeLogService, jej�ch 2 metod,
jejich vstupn�ch a v�stupn�ch parametr�. V build procesu se z n� vygeneruje abstraktn�
t��da CumulativeLog.CumulativeLogBase, kter� m� 2 abstraktn� metody (popis v .proto), 
kter� v souboru CumulativeLogService.cs implementuji v jej�m potomkovi. 
Tak nadefinuji, co ta slu�ba (jej� metody) bude d�lat.

Defaultn� nastaven� je takov�, �e pro ka�d� vol�n� metody n�jak� slu�by se na serveru 
vytvo�� nov� instance t�to slu�by (CumulativeLogService), proto m�m tu kumulativn�
prom�nnou statickou (cumulativeText)

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