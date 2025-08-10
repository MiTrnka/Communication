using HotChocolate.Types; // Pot�ebujeme pro [ExtendObjectType]

namespace GraphQL;
// Doinstaloval jsem si NuGet bal��ky HotChocolate.AspNetCore a HotChocolate.Data

// T��da reprezentuj�c� osobu - Z�ST�V� BEZE ZM�NY
public class Person
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int Age { get; set; }

    // V�imni si, �e tato property je nyn� pr�zdn�. Nebude se plnit v PersonService,
    // ale a� na vy��d�n� pomoc� specializovan�ho resolveru.
    public List<Car> Cars { get; set; } = new();
}

// T��da reprezentuj�c� auto - Z�ST�V� BEZE ZM�NY
public class Car
{
    public int Id { get; set; }
    public string Make { get; set; }
    public string Model { get; set; }
    public int Year { get; set; }
    public int OwnerId { get; set; }
}

// Slu�ba pro pr�ci s daty - ZDE JSOU D�LE�IT� ZM�NY
public class PersonService
{
    // Intern� �lo�i�t� z�st�vaj� stejn�
    private readonly List<Person> _people = new()
    {
        new Person { Id = 1, FirstName = "Jan", LastName = "Nov�k", Age = 30 },
        new Person { Id = 2, FirstName = "Petra", LastName = "Svobodov�", Age = 25 }
    };

    private readonly List<Car> _cars = new()
    {
        new Car { Id = 1, Make = "�koda", Model = "Octavia", Year = 2015, OwnerId = 1 },
        new Car { Id = 2, Make = "Volkswagen", Model = "Golf", Year = 2018, OwnerId = 1 },
        new Car { Id = 3, Make = "Toyota", Model = "Corolla", Year = 2020, OwnerId = 2 }
    };

    // --- ZM�NA #1: ZJEDNODU�EN� METOD ---
    // Tato metoda nyn� vrac� POUZE seznam osob. Logiku pro na��t�n� aut jsme odstranili,
    // proto�e o tu se postar� specializovan� resolver, a to jen v p��pad�, �e si klient o auta �ekne.
    public IEnumerable<Person> GetAll()
    {
        Console.WriteLine("PersonService.GetAll: Vrac�m pouze seznam osob.");
        return _people;
    }

    // Stejn� tak tato metoda vrac� jen z�kladn� data o jedn� osob�.
    public Person? GetById(int id)
    {
        Console.WriteLine($"PersonService.GetById({id}): Vrac�m pouze data pro osobu s ID {id}.");
        return _people.FirstOrDefault(p => p.Id == id);
    }

    // --- NOV� SPECI�LN� METODA ---
    // P�idali jsme malou, jedno��elovou metodu, kter� um� jen jednu v�c:
    // naj�t auta pro dan� ID vlastn�ka. Tuto metodu bude volat n� car resolver.
    public IEnumerable<Car> GetCarsByOwnerId(int ownerId)
    {
        Console.WriteLine($"PersonService.GetCarsByOwnerId({ownerId}): Hled�m auta pro vlastn�ka s ID {ownerId}.");
        return _cars.Where(car => car.OwnerId == ownerId);
    }

    // Metoda AddPerson z�st�v� v principu stejn�, p�id� data do "datab�ze".
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

// --- ZM�NA #2: P�ID�N� T��DY S RESOLVERY ---
// Tato nov� t��da obsahuje "specialisty" (resolvery), kte�� roz�i�uj� n� z�kladn� typ Person.
// Atribut [ExtendObjectType] ��k� Hot Chocolate: "Metody v t�to t��d� p�id�vaj� pole nebo logiku
// k GraphQL typu 'Person', kter� byl p�vodn� vytvo�en z C# t��dy Person."
[ExtendObjectType(typeof(Person))]
public class PersonResolvers
{
    // RESOLVER #1: Specialista na auta
    // Tato metoda se spust� POKA�D�, kdy� si klient v dotazu vy��d� pole 'cars' u objektu Person.
    // N�zev metody 'GetCars' je konvence (Get + n�zev property) a pr�v� ten suffix Cars definuje, �e se m� pro cars spustit tato metoda (resolver).
    // Mohl bych ale na m�sto t�to konvence pou��t atribut [GraphQLName("cars")] a pojmenovat pak metodu jakkoliv.
    public IEnumerable<Car> GetCars([Parent] Person person, [Service] PersonService service)
    {
        // [Parent] Person person: Toto je kl��ov�. Hot Chocolate n�m sem automaticky p�ed� instanci
        // "rodi�ovsk�ho" objektu, tedy t� konkr�tn� osoby, pro kterou pr�v� �e��me jej� auta.
        // [Service] PersonService service: Toto je klasick� dependency injection, dostaneme sem na�i slu�bu.

        Console.WriteLine($"RESOLVER: Spou�t�m resolver pro 'cars' u osoby '{person.FirstName}'.");
        return service.GetCarsByOwnerId(person.Id);
    }

    // RESOLVER #2: Specialista na cel� jm�no (dynamicky generovan� pole)
    // Tento resolver vytvo�� v na�em GraphQL sch�matu �pln� nov� pole 'fullName',
    // kter� v C# t��d� Person v�bec neexistuje.
    public string GetFullName([Parent] Person person)
    {
        Console.WriteLine($"RESOLVER: Spou�t�m resolver pro 'fullName' u osoby '{person.FirstName}'.");
        return $"{person.FirstName} {person.LastName}";
    }
}

// Dotazovac� t��da - Z�ST�V� BEZE ZM�NY
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
    public IEnumerable<Person> GetPeople() => _personService.GetAll();

    [GraphQLName("GetPersonById")]
    public Person? GetPersonById(int id) => _personService.GetById(id);
}

// Vstupn� typ pro auto a muta�n� t��da - Z�ST�VAJ� BEZE ZM�NY
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

// Hlavn� program - JEDNA D�LE�IT� ZM�NA V NASTAVEN�
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
            // --- ZM�NA #3: REGISTRACE RESOLVER� ---
            // T�mto ��dkem ��k�me GraphQL serveru: "A� bude� sestavovat sch�ma,
            // pod�vej se tak� do t��dy PersonResolvers a pou�ij jej� metody
            // k roz���en� typu Person." Bez tohoto ��dku by server o na�ich
            // specialistech nev�d�l.
            .AddTypeExtension<PersonResolvers>()
            .AddFiltering()
            .AddSorting();

        var app = builder.Build();
        app.MapGraphQL();
        app.Run();
    }
}

/*
P��KLADY NOV�CH DOTAZ�, KTER� JSOU TE� MO�N�

// DOTAZ #1: CHCI JEN CEL� JM�NA, BEZ AUT
// Zde se spust� Query.GetPeople a pro ka�dou osobu se spust� resolver GetFullName.
// Resolver GetCars se V�BEC NESPUST�! Server tak u�et�� pr�ci.
query {
    GetPeople {
        id
        fullName
    }
}

// DOTAZ #2: CHCI CEL� JM�NO A K TOMU AUTA
// Zde se spust� Query.GetPeople a pro ka�dou osobu se spust�
// resolver GetFullName A Z�ROVE� i resolver GetCars.
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

DOTAZ NA V�ECHNY OSOBY V�ETN� JEJICH AUT
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

DOTAZ NA KONKRETN� OSOBU
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

VLO�� OSOBU SE 2 AUTY
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