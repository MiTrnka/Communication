/*
přidán NuGet balíček RabbitMQ.Client

Tovární objekt a spojení: Stejně jako u producenta, i zde vytváříme instanci 
ConnectionFactory a používáme ji k vytvoření spojení s RabbitMQ serverem.
Deklarace fronty: Důležité je, aby název fronty a její vlastnosti byly konzistentní 
mezi producentem a konzumentem. Jakákoli nesrovnalost by mohla vést k nedorozuměním 
nebo chybám.
Konzumace zpráv: Vytvoření EventingBasicConsumer a registrace události Received umožňuje 
aplikaci reagovat na příchozí zprávy. Jakmile přijde zpráva, vykoná se tělo události Received.
Auto-acknowledgement: Ve výchozím nastavení je autoAck nastaven na true, což znamená, 
že RabbitMQ automaticky odstraní zprávu z fronty jakmile ji doručí konzumentovi. 
Pokud je potřeba zpracování zprávy ovládat detailněji

*/
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;

namespace RabitMQConsumer;

internal class Program
{
    public static async Task Main(string[] args)
    {
        // Vytvoření továrního objektu pro spojení s RabbitMQ, podobně jako u producentské aplikace.
        var factory = new ConnectionFactory() { HostName = "localhost" };

        // Vytvoření spojení a kanálu pro komunikaci s RabbitMQ.
        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel()) // Kanály jsou lehké a doporučuje se pro každou operaci vytvořit nový.
        {
            // Deklarace fronty, ze které bude aplikace přijímat zprávy.
            // Fronta by měla být deklarována identicky v obou aplikacích (producer a consumer).
            channel.QueueDeclare(queue: "MichalovaFronta",
                                 durable: false, // Změňte na true, pokud potřebujete, aby zprávy přežily restart serveru.
                                 exclusive: false, // Změňte na true, pokud frontu má používat pouze jedno spojení.
                                 autoDelete: false, // Změňte na true, pokud chcete, aby se fronta automaticky smazala po odpojení posledního konzumenta.
                                 arguments: null);

            // Vytvoření konzumenta, který bude přijímat zprávy z fronty.
            //model reprezentuje kanál AMQP, na kterém byla zpráva přijata
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray(); // Převedení zprávy z pole bajtů zpět na řetězec.
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine(" Obdržená zpráva: {0}", message); // Výpis přijaté zprávy na konzolu.

                // Nepovinné potvrzení zprávy - indikuje RabbitMQ, že zpráva byla úspěšně zpracována a může být odstraněna z fronty.
                // Toto je relevantní pouze pokud je autoAck nastaveno na false.
                //((IModel)model).BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
            };

            // Spuštění naslouchání na zprávy v frontě. 
            //Trochu matoucí, že to je na konci, ale na začátku se vše nastavilo a teď se spustí.
            // autoAck: true znamená, že server automaticky potvrdí přijetí zprávy. Změňte na false, pokud chcete potvrzovat zprávy manuálně.
            channel.BasicConsume(queue: "MichalovaFronta",
                                 autoAck: true, // Pokud nastavíte na false, musíte explicitně potvrdit zprávy pomocí channel.BasicAck.
                                 consumer: consumer);
            /*
             autoAck: Tento parametr řídí, zda se má potvrzení o přijetí zprávy odesílat automaticky 
             zpět do RabbitMQ. Když je nastaven na true, RabbitMQ okamžitě odstraní zprávu z fronty 
            jakmile ji doručí konzumentovi. To znamená, že pokud by váš konzument přestal fungovat 
            před zpracováním zprávy, zpráva by byla ztracena. Pokud je autoAck nastaven na false, 
            musíte explicitně zavolat channel.BasicAck po úspěšném zpracování zprávy, aby bylo 
            RabbitMQ informováno, že zprávu lze bezpečně odstranit z fronty. 
            Toto nastavení se používá pro zajištění spolehlivého zpracování zpráv, kdy se zprávy 
            odstraňují pouze po potvrzení, že byly zpracovány.

            consumer: Instance EventingBasicConsumer (nebo jiného konzumenta), který obsahuje 
            logiku pro zpracování přijatých zpráv. Jakmile je tato metoda zavolána s instancí 
            konzumenta, začne RabbitMQ doručovat zprávy z uvedené fronty do vaší aplikace, kde 
            jsou zpracovávány prostřednictvím události Received.
            */

            await Console.In.ReadLineAsync(); // Asynchronní čekání na stisk klávesy Enter, aby se aplikace neukončila ihned.
        }
    }
}
