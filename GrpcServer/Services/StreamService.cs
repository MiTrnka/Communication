using Grpc.Core;
using GrpcServer;
using System.Threading.Tasks;
using System.Threading;

namespace gRPCServer.Services;

/*
Služba pro streamování, každou vteřinu odešle svému klientovi (může jich být neomezeně
a každý začíná od začátku s messageId = 0) zprávu, dokud není spojení ukončeno tím, že
klient ukončí spojení nebo server zruší službu. 
*/

public class StreamingServiceImpl : StreamingService.StreamingServiceBase
{
    //Každý klient, který volá tuto metodu, má jiný context.
    public override async Task StreamMessages(Empty request, IServerStreamWriter<StreamResponse> responseStream, ServerCallContext context)
    {
        int messageId = 0;
        
        // Dokud není spojení ukončeno (dokut klient žije), odesílejte zprávy
        while (!context.CancellationToken.IsCancellationRequested)
        {
            await responseStream.WriteAsync(new StreamResponse { Message = $"IPAdresaIPV6:{context.Peer} Message {++messageId}" });
            await Task.Delay(1000); // Počkejte 1 sekundu před odesláním další zprávy
        }
    }
}
