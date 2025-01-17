using RESTAPIClient;
using System.Net.Http.Json;

//Vytvorim si HTTP klienta a nastavim mu uvodni cast uri, u konkretnich Get, Post... volanich doplnim zbytek uri
var client = new HttpClient();
client.BaseAddress = new Uri("http://localhost:5293/api/");
//Post: Vytvoreni nove kocky s Id 10
Console.WriteLine("Vytvoreni nove kocky s Id 10");
var response1 = await client.PostAsJsonAsync("cats", new Cat { Id = 10, Name = "Nova Kocka" });
if (response1.IsSuccessStatusCode)
{
    // Získání URL na API z Location hlavičky (v API je to prvni parametr metody CreatedAtAction)
    string locationUrl = response1.Headers.Location.ToString();
    Console.WriteLine("URL pro nove vytvorenou kocku: " + locationUrl);

    //Ziskani prave zalozene kocky z body (v API je to druhy parametr metody CreatedAtAction)
    var kocka1 = await response1.Content.ReadFromJsonAsync<Cat>();
    Console.WriteLine($"Prave vytvorena kocka: id: {kocka1.Id} Jmeno kocky: {kocka1.Name}");
}


//Put: Aktualizace kocky s Id 10
Console.WriteLine("Aktualizace kocky s Id 10");
await client.PutAsJsonAsync("cats/10", new Cat { Id = 10, Name = "Nova Kocka po update" });

//Get: Zavolani REST API Get na konkretni kocku s Id 10
Console.WriteLine("Zavolani REST API Get na konkretni kocku s Id=10");
var response2 = await client.GetAsync("cats/10");
var kocka2 = await response2.Content.ReadFromJsonAsync<Cat>();
Console.WriteLine($"id: {kocka2?.Id} Jmeno kocky: {kocka2?.Name}");

//Del: Smazani kocky s Id 10
await client.DeleteAsync("cats/10");

//Get: Zavolani REST API Get na vsechny kocky po smazani kocky s Id 10
Console.WriteLine("Zavolani REST API Get na vsechny kocky po smazani kocky s Id 10");
var response3 = await client.GetAsync("cats");
var kocky = await response3.Content.ReadFromJsonAsync<IEnumerable<Cat>>();
foreach (var kocka3 in kocky)
    Console.WriteLine($"id: {kocka3.Id} Jmeno kocky: {kocka3.Name}");

