using Microsoft.AspNetCore.SignalR;
namespace SignalRServer;

/*
Třída ChatHub dědí od Hub. To ji činí centrálním bodem pro komunikaci v SignalR.
List<string> _messages: Statická proměnná pro uchování historie zpráv.
*/
public class ChatHub : Hub
{
    /* Jen rozesila vsem
    public async Task SendMessage(string user, string message)
    {
        await Clients.All.SendAsync("ReceiveMessage", user, message);
    }*/

    //Seznam všech zpráv
    private static readonly List<string> _messages = new List<string>();

    /*Tato přepsaná metoda se volá, když se klient připojí k Hubu.
    Používá se k odeslání historie zpráv právě připojenému klientu.*/
    public override async Task OnConnectedAsync()
    {
        // Získání ID nově připojeného klienta, to pak mohu použít
        //například pro odeslání zprávy jen tomuto klientovi
        string connectionId = Context.ConnectionId;

        // Odeslání všech dosud uložených zpráv nově připojenému klientovi
        await Clients.Caller.SendAsync("ReceiveMessages", _messages);
        await base.OnConnectedAsync();
    }

    //Tato přepsaná metoda se volá, když se klient odpojí od Hubu
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        // Zde můžete provést potřebné úklidové operace
        await base.OnDisconnectedAsync(exception);
    }

    /*Metoda, kterou klienti volají, když chtějí odeslat zprávu.
    Zpráva je přidána do seznamu _messages a následně odeslána všem připojeným klientům
    pomocí Clients.All.SendAsync()*/
    public async Task SendMessage(string user, string message)
    {
        var fullMessage = $"{user}: {message}";
        _messages.Add(fullMessage); // Uložení zprávy

        // Odeslání zprávy všem klientům, ale jen ti klienti, kteří si danou obslužnou
        // metodu zaregistrovali, tak ti ji u sebe vykonají
        await Clients.All.SendAsync("ReceiveMessage", user, message);

        //Volám dvakrát testovací obslužnou metodu zaregistrovanou na klientovi
        await Clients.All.SendAsync("ReceivePrazdnaZprava");
        await Clients.AllExcept(Context.ConnectionId).SendAsync("ReceivePrazdnaZprava"); // Vyjme volajícího

        //Na klientovi musí sedět parametry metody, jinak se metoda
        //neprovede a vyvolá se výjimka na klientovi
        await Clients.All.SendAsync("ReceiveCislo", 42);

        /* Níže jsou způsoby, jak poslat zprávu jen určitým klientům
        await Clients.Group("groupName").SendAsync("ReceivePrazdnaZprava");
        await Clients.Client(connectionId).SendAsync("ReceivePrazdnaZprava");
        await Clients.AllExcept(new List<string> { connectionId }).SendAsync("ReceivePrazdnaZprava");*/
    }
    public async Task<int> AktualniPocetZnaku()
    {
        return _messages.Sum(m => m.Length);
    }
}