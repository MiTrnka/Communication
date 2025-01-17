// Doinstalovat balíček Newtonsoft.Json
namespace GraphQLClient;

using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Hlavní třída programu
public class Program
{
    // Asynchronní hlavní metoda, která umožňuje používat await
    public static async Task Main(string[] args)
    {
        // Definujeme URL GraphQL endpointu. Pokud se port liší, upravte.
        string graphqlEndpoint = "http://localhost:5057/graphql";

        // Vytvoříme instanci HttpClient, která nám umožní posílat HTTP požadavky
        // Konfigurace HttpClient s vypnutým ověřováním certifikátů (pouze pro lokální testování)
        using (var httpClientHandler = new HttpClientHandler())
        {
            httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                return true; // Povolíme všechny certifikáty, je to nebezpečné v produkci
            };

            using (var httpClient = new HttpClient(httpClientHandler))
            {
                // Zavolání GraphQL query pro získání všech lidí
                await ExecuteQuery(httpClient, graphqlEndpoint, GetAllPeopleQuery, "Všichni lidé:").ConfigureAwait(false);

                // Zavolání GraphQL query pro získání konkrétní osoby
                await ExecuteQuery(httpClient, graphqlEndpoint, GetPersonByIdQuery(1), "Osoba s ID 1:").ConfigureAwait(false);

                // Zavolání GraphQL mutation pro přidání osoby
                var addPersonMutation = CreateAddPersonMutation("Karel", "Gott", 80,
                                                                new CarInput("Mercedes", "S class", 2022),
                                                                new CarInput("Ferrari", "F40", 1987));
                await ExecuteQuery(httpClient, graphqlEndpoint, addPersonMutation, "Přidána nová osoba:").ConfigureAwait(false);

                // Znovu načteme seznam lidí, abychom viděli nově přidanou osobu.
                await ExecuteQuery(httpClient, graphqlEndpoint, GetAllPeopleQuery, "Všichni lidé (po přidání):").ConfigureAwait(false);

                Console.WriteLine("\nStiskněte libovolnou klávesu pro ukončení...");
                Console.ReadKey();
            }
        }
    }

    // Metoda pro vykonání GraphQL dotazu
    private static async Task ExecuteQuery(HttpClient httpClient, string graphqlEndpoint, string query, string messagePrefix)
    {
        // Připravíme obsah HTTP požadavku, což je JSON s GraphQL dotazem
        var content = new StringContent(JsonConvert.SerializeObject(new { query }), Encoding.UTF8, "application/json");

        // Pošleme POST požadavek na GraphQL endpoint s dotazem
        var response = await httpClient.PostAsync(graphqlEndpoint, content).ConfigureAwait(false);

        // Zkontrolujeme, jestli byl požadavek úspěšný (stavový kód 2xx)
        //response.EnsureSuccessStatusCode();

        // Přečteme obsah odpovědi jako řetězec
        var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        // Zpracujeme JSON odpověď pomocí Newtonsoft.Json
        var jsonResponse = JObject.Parse(responseBody);

        // Získáme data z výsledku (předpokládáme, že data jsou uvnitř klíče "data")
        var data = jsonResponse["data"];

        // Vypíšeme zprávu na konzoli
        Console.WriteLine($"\n{messagePrefix}");

        // Vypíšeme data na konzoli, data mohou být null
        if (data != null)
        {
            Console.WriteLine(data.ToString(Newtonsoft.Json.Formatting.Indented));
        }
        else
        {
            Console.WriteLine("Žádná data nenalezena.");
        }
    }

    // Definice GraphQL query pro získání všech lidí
    private const string GetAllPeopleQuery = @"
        query {
            GetPeople {
                id
                firstName
                lastName
                age
                cars {
                    id
                    make
                    model
                    year
                }
            }
        }
    ";

    // Definice GraphQL query pro získání osoby podle ID
    private static string GetPersonByIdQuery(int id)
    {
        return $@"
            query {{
                GetPersonById(id: {id}) {{
                    firstName
                    lastName
                    age
                    cars {{
                        make
                        model
                    }}
                }}
            }}
        ";
    }

    // Definice GraphQL mutation pro přidání osoby
    private static string CreateAddPersonMutation(string firstName, string lastName, int age, params CarInput[] cars)
    {
        // Převedeme CarInput do formátu, který GraphQL akceptuje
        var carsJson = JsonConvert.SerializeObject(cars);

        return $@"
            mutation {{
                addPerson(
                    firstName: ""{firstName}"",
                    lastName: ""{lastName}"",
                    age: {age},
                    cars: {carsJson}
                ) {{
                    id
                    firstName
                    lastName
                    age
                    cars {{
                        id
                        make
                        model
                        year
                    }}
                }}
            }}
        ";
    }


    // Vstupní typ pro auto, odpovídající GraphQL definici
    public record CarInput(string Make, string Model, int Year);
}