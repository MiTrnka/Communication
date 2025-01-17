/*
Klientská část
Projekt založen jako: Konzolová aplikace
NuGet balíčky: Grpc.Net.Client, Google.Protobuf, Grpc.Tools
Nakopírovat ze serverové aplikace .proto soubory (nebo přidat Add as link) a nastavit jim v properties:
.proto build action: Protobuf compiler, gRPC Stubb classes: Client only

Vytvořím napojení (chanel) na server a vytvořím klienty pro volání metod.
Každý klient odpovídá jedne službě (service) na serveru. Ty jsou popsány v .proto souborech.

Metody jsou volány asynchronně (doporučeno). 
*/
using Grpc.Core;
using Grpc.Net.Client;
using gRPCServer;

namespace gRPCClient;

internal class Program
{
    static async Task Main(string[] args)
    {
        //S P O L E Č N Á  Č Á S T
        // Vytvoření gRPC kanálu na danou adresu
        using var channel = GrpcChannel.ForAddress("https://localhost:7167");


        // P O U Ž I T Í  S L U Ž B Y  G R E E T E R S E R V I C E
        Console.WriteLine("P O U Ž I T Í  S L U Ž B Y  G R E E T E R S E R V I C E");
        // Vytvoření klienta pro danou službu
        var clientGreeter = new Greeter.GreeterClient(channel);
        // Volání serverové metody SayHello
        var reply = await clientGreeter.SayHelloAsync(new HelloRequest { Name = "abc", Age = 30 });
        Console.WriteLine("Zprava: " + reply.Message);
        Console.WriteLine("");
        

        // P O U Ž I T Í  S L U Ž B Y   C U M U L A T I V E  L O G
        Console.WriteLine("P O U Ž I T Í  S L U Ž B Y   C U M U L A T I V E  L O G");
        // Vytvoření klienta pro danou službu
        var clientCumulativeLog = new CumulativeLog.CumulativeLogClient(channel);
        // Volání metody Add
        var addResponse = await clientCumulativeLog.AddAsync(new AddRequest { Text = "Prvni" });
        Console.WriteLine($"Nová hodnota kumulativní proměnné na serveru: {addResponse.Text}");
        // Volání metody Insert
        var insertResponse = await clientCumulativeLog.InsertAsync(new InsertRequest { Text = "Druhy", Index = 2 });
        Console.WriteLine($"Nová hodnota kumulativní proměnné na serveru: {insertResponse.Text}");
        Console.WriteLine("");


        // P O U Ž I T Í  S L U Ž B Y   S T R E A M  S E R V I C E
        Console.WriteLine("P O U Ž I T Í  S L U Ž B Y   S T R E A M  S E R V I C E");
        var clientStreamService = new StreamingService.StreamingServiceClient(channel);
        //streamMessageResponse je proměnná reprezentující gRPC volání, které bylo zahájeno na klientovi pro
        //určitou metodu služby, například metodu StreamMessages. Toto volání obsahuje vícero
        //vlastností a metod souvisejících s tímto voláním, včetně ResponseStream
        using var streamMessageResponse = clientStreamService.StreamMessages(new gRPCServer.Empty());
        //Kombinace await foreach a ReadAllAsync() v tomto příkladu efektivně a neblokujícím
        //způsobem zpracovává stream zpráv ze serveru. Pro každou zprávu, která je asynchronně
        //přijata z ResponseStream, se vykoná tělo smyčky
        //ReadAllAsync() je asynchronní metoda, která umožňuje čtení všech zpráv ze streamu,
        //dokud stream nekončí. Jedná se o rozšiřující (extension) metodu
        //poskytovanou balíčkem System.Linq.Async
        //await foreach je syntaxe v C# 8.0 a vyšším, která umožňuje asynchronně iterovat
        //přes prvky získané z asynchronního enumerátoru. V tomto případě await foreach
        //asynchronně čte každou zprávu získanou z ReadAllAsync(), který prochází ResponseStream
        await foreach (var message in streamMessageResponse.ResponseStream.ReadAllAsync())
        {
            Console.WriteLine("Přijato od serveru: " + message.Message);
        }

    }
}
