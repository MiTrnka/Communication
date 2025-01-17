/*
Aby zasílání zpráv fungovalo, je potřeba nainstalovat message broaker RabbitMQ
1. Instalace programovacího jazyka Erlang
2. Instalace RabbitMQ serveru (výchozí uživatel guest má heslo guest a je přístupný pouze z localhost)
3. spustit cmd jako správce a jít do C:\Program Files\RabbitMQ Server\rabbitmq_server-3.13.0\sbin
4. spustit tam pŕíkaz: rabbitmq-plugins enable rabbitmq_management
5. restartovat RabbitMQ server (nebo počítač)
6. Naprogramovat konzolové aplikace producenta a konzumenta

přidán NuGet balíček RabbitMQ.Client
*/
using System;
using System.Text;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace RabitMQProducer;

internal class Program
{
    public static async Task Main(string[] args)
    {
        // Vytváří tovární objekt pro vytvoření spojení s RabbitMQ. HostName určuje, kde RabbitMQ server běží.
        // V případě potřeby můžete nastavit další vlastnosti factory, jako jsou UserName a Password.
        var factory = new ConnectionFactory() { HostName = "localhost" };

        // Vytvoření spojení s RabbitMQ serverem.
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel()) // Vytvoření kanálu, přes který budou odesílány zprávy.
        {
            // Deklarace fronty, do které budou zprávy odesílány. Pokud fronta neexistuje, bude vytvořena.
            // Parametry durable, exclusive a autoDelete ovlivňují chování fronty:
            // durable: pokud je true, fronta přežije restart serveru.
            // exclusive: pokud je true, fronta bude použitelná pouze pro jedno spojení a bude smazána po jeho uzavření.
            // autoDelete: pokud je true, fronta bude automaticky smazána, jakmile nebude mít žádné konzumenty.
            channel.QueueDeclare(queue: "MichalovaFronta",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            // Tělo zprávy. Tento příklad odesílá jednoduchou textovou zprávu.
            string message = "Toto je obsah zprávy vygenerovaný producentem";
            var body = Encoding.UTF8.GetBytes(message); // Převod zprávy na pole bajtů.

            // Odesílání zprávy do fronty.
            // exchange: Určuje, do které výměny bude zpráva odeslána. Prázdný řetězec znamená použití výchozí výměny.
            // routingKey: Název fronty, do které chceme zprávu odeslat. V tomto případě "MichalovaFronta".
            channel.BasicPublish(exchange: "",
                                 routingKey: "MichalovaFronta",
                                 basicProperties: null, // Další vlastnosti zprávy, např. priorita, zpoždění atd.
                                 body: body);
            await Task.Delay(3000);
            //Po 3 vteřinách se odešle ta zpráva znovu
            channel.BasicPublish(exchange: "",
                     routingKey: "MichalovaFronta",
                     basicProperties: null, // Další vlastnosti zprávy, např. priorita, zpoždění atd.
                     body: body);


            Console.WriteLine("Zpráva byla producentem právě odeslána");
        }

        await Console.In.ReadLineAsync(); // Asynchronní čekání na stisk klávesy Enter, aby se aplikace neukončila ihned.
    }
}
