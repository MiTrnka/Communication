using HotChocolate.Types; // Potøebujeme pro [ExtendObjectType]

namespace GraphQL;
// Doinstaloval jsem si NuGet balíèky HotChocolate.AspNetCore a HotChocolate.Data

// Tøída reprezentující osobu - ZÙSTÁVÁ BEZE ZMÌNY
public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int Age { get; set; }

    // Všimni si, že tato property je nyní prázdná. Nebude se plnit v PersonService,
    // ale až na vyžádání pomocí specializovaného resolveru.
    public List<Car> Cars { get; set; } = new();
}

// Tøída reprezentující auto
public class Car
{
    public int Id { get; set; }
    public string Make { get; set; }
    public string Model { get; set; }
    public int Year { get; set; }
    public int OwnerId { get; set; }
}

// Služba pro práci s daty
public class PersonService
{
    // Interní úložištì zùstávají stejná
    private readonly List<Person> _people = new()
    {
        new Person { Id = 1, FirstName = "Jan", LastName = "Novák", Age = 30 },
        new Person { Id = 2, FirstName = "Petra", LastName = "Svobodová", Age = 25 }
    };

    private readonly List<Car> _cars = new()
    {
        new Car { Id = 1, Make = "Škoda", Model = "Octavia", Year = 2015, OwnerId = 1 },
        new Car { Id = 2, Make = "Volkswagen", Model = "Golf", Year = 2018, OwnerId = 1 },
        new Car { Id = 3, Make = "Toyota", Model = "Corolla", Year = 2020, OwnerId = 2 }
    };

    // Tato metoda vrací POUZE seznam osob. Logiku pro naèítání aut bude na vyžádání øešit specializovaný resolver
    // a to jen v pøípadì, že klient dotazu skuteènì vyžádá pole 'cars'.
    public IEnumerable<Person> GetAllPeople()
    {
        return _people;
    }

    // Stejnì tak tato metoda vrací jen základní data o jedné osobì.
    public Person? GetPersonById(int id)
    {
        return _people.FirstOrDefault(p => p.Id == id);
    }

    // Najde auta pro dané ID vlastníka. Tuto metodu bude volat náš car resolver.
    public IEnumerable<Car> GetCarsByOwnerId(int ownerId)
    {
        return _cars.Where(car => car.OwnerId == ownerId);
    }

    // Pøidá osobu s jejími auty do interního úložištì.
    public Person AddPerson(Person person)
    {
        person.Id = _people.Max(p => p.Id) + 1;
        foreach (var car in person.Cars)
        {
            car.Id = _cars.Max(c => c.Id) + 1;
            car.OwnerId = person.Id;
            _cars.Add(car);
        }
        _people.Add(person);
        return person;
    }
}

// --- TØÍDA S RESOLVERY ---
// Tato tøída obsahuje "specialisty" (resolvery), kteøí rozšiøují náš základní typ Person.
// Atribut [ExtendObjectType] øíká Hot Chocolate: "Metody v této tøídì pøidávají pole nebo logiku
// k GraphQL typu 'Person', který byl pùvodnì vytvoøen z C# tøídy Person."
// Název samotné tøídy PersonResolvers je konvence, ale není povinný.
[ExtendObjectType(typeof(Person))]
public class PersonResolvers
{
    // RESOLVER #1: Specialista na auta
    // Tato metoda se spustí POKAŽDÉ, když si klient v dotazu vyžádá pole 'cars' u objektu Person.
    // Název metody 'GetCars' je konvence (Get + název property) a právì ten suffix Cars definuje, že se má pro cars spustit tato metoda (resolver).
    // Mohl bych ale na místo této konvence použít atribut [GraphQLName("cars")] a pojmenovat pak metodu jakkoliv.
    public IEnumerable<Car> GetCars([Parent] Person person, [Service] PersonService service /*pro efektivnejsi resolver injektujeme na místo PersonService svùj DataLoader, který zamezí N+1 problému*/)
    {
        // [Parent] Person person: Toto je klíèové. Hot Chocolate nám sem automaticky pøedá instanci
        // "rodièovského" objektu, tedy té konkrétní osoby, pro kterou právì øešíme její auta.
        // [Service] PersonService service: Toto je klasická dependency injection, dostaneme sem naši službu.

        Console.WriteLine($"RESOLVER: Spouštím resolver pro 'cars' u osoby '{person.FirstName}'.");
        return service.GetCarsByOwnerId(person.Id);
    }

    // RESOLVER #2: Specialista na celé jméno (dynamicky generované pole)
    // Tento resolver vytvoøí v našem GraphQL schématu úplnì nové pole 'fullName',
    // které v C# tøídì Person vùbec neexistuje.
    public string GetFullName([Parent] Person person)
    {
        Console.WriteLine($"RESOLVER: Spouštím resolver pro 'fullName' u osoby '{person.FirstName}'.");
        return $"{person.FirstName} {person.LastName}";
    }
}

// Dotazovací tøída - ZÙSTÁVÁ BEZE ZMÌNY
public class Query
{
    private readonly PersonService _personService;

    public Query(PersonService personService)
    {
        _personService = personService;
    }

    [UseFiltering]
    [UseSorting]
    [GraphQLName("GetPeople")]
    public IEnumerable<Person> GetPeople() => _personService.GetAllPeople();

    [GraphQLName("GetPersonById")]
    public Person? GetPersonById(int id) => _personService.GetPersonById(id);
}

// Vstupní typ pro auto a mutaèní tøída - ZÙSTÁVAJÍ BEZE ZMÌNY
public record CarInput(string Make, string Model, int Year);
public class Mutation
{
    private readonly PersonService _personService;
    public Mutation(PersonService personService)
    {
        _personService = personService;
    }
    public Person AddPerson(string firstName, string lastName, int age, List<CarInput> cars)
    {
        var person = new Person
        {
            FirstName = firstName,
            LastName = lastName,
            Age = age,
            Cars = cars.Select(car => new Car
            {
                Make = car.Make,
                Model = car.Model,
                Year = car.Year
            }).ToList()
        };
        return _personService.AddPerson(person);
    }
}

// Hlavní program - JEDNA DÙLEŽITÁ ZMÌNA V NASTAVENÍ
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<PersonService>();

        builder.Services
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMutationType<Mutation>()
            // Tímto øádkem øíkáme GraphQL serveru: "Až budeš sestavovat schéma,
            // podívej se také do tøídy PersonResolvers a použij její metody
            // k rozšíøení typu Person." Bez tohoto øádku by server o našich
            // specialistech nevìdìl.
            .AddTypeExtension<PersonResolvers>()
            .AddFiltering()
            .AddSorting();

        var app = builder.Build();
        app.MapGraphQL();
        app.Run();
    }
}

/*
PØÍKLADY NOVÝCH DOTAZÙ, KTERÉ JSOU TEÏ MOŽNÉ

// DOTAZ #1: CHCI JEN CELÁ JMÉNA, BEZ AUT
// Zde se spustí Query.GetPeople a pro každou osobu se spustí resolver GetFullName.
// Resolver GetCars se VÙBEC NESPUSTÍ! Server tak ušetøí práci.
query {
    GetPeople {
        id
        fullName
    }
}

// DOTAZ #2: CHCI CELÉ JMÉNO A K TOMU AUTA
// Zde se spustí Query.GetPeople a pro každou osobu se spustí
// resolver GetFullName A ZÁROVEÒ i resolver GetCars.
query {
    GetPeople {
        id
        fullName
        cars {
            make
            model
        }
    }
}

DOTAZ NA VŠECHNY OSOBY VÈETNÌ JEJICH AUT
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

DOTAZ NA KONKRETNÍ OSOBU
query {
    GetPersonById(id: 1) {
        firstName
        lastName
        age
        cars {
            make
            model
        }
    }
}

VLOŽÍ OSOBU SE 2 AUTY
mutation {
  addPerson(
    firstName: "Michal",
    lastName: "Trnka",
    age: 40,
    cars: [
      { make: "Dacia", model: "Duster", year: 2019 },
      { make: "Buggy", model: "custom", year: 2025 }
    ]
  ) {
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

*/