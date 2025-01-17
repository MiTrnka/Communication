using Grpc.Core;
using GrpcServer;
using System.Text;

namespace gRPCServer.Services;
public class CumulativeLogService : CumulativeLog.CumulativeLogBase
{
    private static StringBuilder cumulativeText = new StringBuilder();

    //Metoda přidá do serverové kumulativní proměnné nějaký řetězec na konkrétní místo a vrátí celou tu kumulativní proměnnou
    //Doporučená asynchronní verze metody. Sice tu asynchronnost momentálně nevyužívám,
    //ale je to dobrý zvyk a příprava pro budoucí rozšíření. Asynchronní verze má malinko
    //větší režii, ale v tomto případě je to zanedbatelné.
    public override async Task<LogResponse> Insert(InsertRequest request, ServerCallContext context)
    {
        // Vložení textu na specifikovanou pozici
        if (request.Index < 0 || request.Index > cumulativeText.Length)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Index is out of range."));
        }
        cumulativeText.Insert(request.Index, request.Text);
        return new LogResponse { Text = cumulativeText.ToString() };
    }
    //Metoda přidá do serverové kumulativní proměnné nějaký řetězec na konec a vrátí celou tu kumulativní proměnnou 
    public override Task<LogResponse> Add(AddRequest request, ServerCallContext context)
    {
        // Přidání textu na konec
        cumulativeText.Append(request.Text);

        return Task.FromResult(new LogResponse { Text = cumulativeText.ToString() });
    }
}
