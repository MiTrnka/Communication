/*
Konzolova aplikace
NuGet balíček: Microsoft.AspNetCore.SignalR.Client
Klientská aplikace si vytvoří spojení na server (instance connection třídy HubConnectionBuilder)
Na této instanci si klient nastaví (pomocí connection.On...), které zprávy chce odebírat.
Server tyto zprávy odesílá typicky s nějakými parametry, například:
pro všechny klienty: await Clients.All.SendAsync("ReceiveMessage", user, message);
nebo pro konkrétního: await Clients.Caller.SendAsync("ReceiveMessages", _messages);
takže klient při nastavování toho odběru musí dodržet ty parametry,
ale co si s nimi udělá je už na něm a definuje si to pomocí lambda funkce.
V této klientské aplikaci si klient zaregistroval odběr "ReceiveMessage",
pomocí kterého si vypisuje hlášky od jiných klientů, které server rozesílá všem klientům
Dále klient může volat i programátorem definované metody na serveru a to:
bez očekávané návratové hodnoty: await connection.SendAsync("SendMessage", user, message);
s očekávanou návratovou hodnotou: pocetZnaku = await connection.InvokeAsync<int>("AktualniPocetZnaku");
*/

using Microsoft.AspNetCore.SignalR.Client;

namespace SignalRClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            /*
            SignalR klient v .NET je vytvořen pomocí HubConnectionBuilder, což je třída,
            která poskytuje fluidní API pro konfiguraci a vytvoření spojení s SignalR hubem na serveru.
            WithUrl: Určuje URL adresu SignalR hubu na serveru. Tato URL musí odpovídat endpointu,
            který je nakonfigurován na serverové straně v MapHub<ChatHub>("/chatHub").
            Build(): Vytváří instanci HubConnection, která se používá pro komunikaci s hubem.
            */
            await using var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost:5214/chatHub", options =>
            {
                options.HttpMessageHandlerFactory = _ => new HttpClientHandler
                {
                    // Toto by mělo být použito pouze pro vývojové účely!
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };
            })
            .WithAutomaticReconnect() //asi 4 pokusy o znovupřipojení
            .Build();

            //Takto si vyrobím metodu ReceivePrazdnaZprava (přihlásím se k odběru
            //pro příjem zpráv ze serveru) ,která bude mít 0 parametrů
            //Na serveru se bude pak volat await Clients.All.SendAsync("ReceivePrazdnaZprava");
            //Vlastně tím říkam, pokud server zavolá tuto metodu, tak se provede to, co je v lambda funkci
            connection.On("ReceivePrazdnaZprava", () =>
            {
                Console.WriteLine("Zavolana ze serveru metoda ReceivePrazdnaZprava");
            });
            //Takto si zaregistruji obslužnou metodu ReceiveCislo pro příjem zpráv
            //ze serveru, která bude mít 1 parametr
            connection.On<int>("ReceiveCislo", (cislo) =>
            {
                Console.WriteLine($"Zavolana ze serveru metoda ReceiveCislo s parametrem {cislo}");
            });

            /*
            Pro přijímání zpráv z hubu, klient registruje obslužné metody, které jsou volány,
            když server pošle zprávu klientovi. To se dělá pomocí metody On<T>,
            kde T je typ dat, který očekáváte, že budete přijímat.
            Lambda funkce (user, message) => { ... } je volána pokaždé, když server pošle zprávu ReceiveMessage.
            */
            connection.On<string, string>("ReceiveMessage", (user, message) =>
            {
                Console.WriteLine($"Zavolana ze serveru metoda ReceiveMessage urcena pro chat a obsahujici: {user}: {message}");
            });


            //Pro zobrazení předchozích hlášek v chatu
            //Tato metoda je volána ze serveru, když se klient připojí k Hubu, na serveru
            //se zavolám virtuální override metoda OnConnectedAsync(), ve které se zavolá
            //await Clients.Caller.SendAsync("ReceiveMessages", _messages);
            connection.On<IEnumerable<string>>("ReceiveMessages", messages =>
            {
                foreach (var message in messages)
                {
                    Console.WriteLine(message);
                }
            });

            //pro navázání spojení s hubem, klient může také ukončit spojení voláním StopAsync
            await connection.StartAsync();

            //Zavolá pomocí InvokeAsync (na rozdíl od SendAsync očekává návratovou hodnotu)
            //uživatelskou metodu (AktualniPocetZnaku) na serveru
            int pocetZnaku = await connection.InvokeAsync<int>("AktualniPocetZnaku");
            Console.WriteLine($"Aktuální počet znaků v chatu: {pocetZnaku}");

            Console.WriteLine("Connected to chat. Enter your name:");
            var user = Console.ReadLine();

            //Skoro nekonečná smyčka, která čte zprávy z konzole a odesílá je na server
            Console.WriteLine("Pro ukonceni napiste konec");
            while (1==1)
            {
                string message = Console.ReadLine();
                if (message == "konec")
                    break;
                await connection.SendAsync("SendMessage", user, message);
            }

            // Ukončení spojení se serverem, na serveru se zavolá metoda OnDisconnectedAsync
            //Spojení by se ukončilo a metoda OnDisconnectedAsync by se zavolala i kdybychom
            //nevolali tuto metodu, ale tímto způsobem se to udělá jistě a mohu pak
            //ještě něco udělat před ukončením spojení
            await connection.StopAsync();
        }
    }
}
