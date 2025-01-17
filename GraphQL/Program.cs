namespace GraphQL;
//Doinstaloval jsem si NuGet balíèky HotChocolate.AspNetCore a HotChocolate.Data

// Tøída reprezentující osobu
public class Person
{
    public int Id { get; set; } // Jedineèný identifikátor osoby
    public string FirstName { get; set; } // Køestní jméno
    public string LastName { get; set; } // Pøíjmení
    public int Age { get; set; } // Vìk
    public List<Car> Cars { get; set; } = new(); // Seznam aut vlastnìných osobou
}

// Tøída reprezentující auto
public class Car
{
    public int Id { get; set; } // Jedineèný identifikátor auta
    public string Make { get; set; } // Znaèka auta
    public string Model { get; set; } // Model auta
    public int Year { get; set; } // Rok výroby
    public int OwnerId { get; set; } // Id vlastníka (osoby)
}

// Služba pro práci s daty osob a aut, simuluje databázi
public class PersonService
{
    // Interní úložištì osob
    private readonly List<Person> _people = new()
    {
        new Person { Id = 1, FirstName = "Jan", LastName = "Novák", Age = 30 },
        new Person { Id = 2, FirstName = "Petra", LastName = "Svobodová", Age = 25 }
    };

    // Interní úložištì aut
    private readonly List<Car> _cars = new()
    {
        new Car { Id = 1, Make = "Škoda", Model = "Octavia", Year = 2015, OwnerId = 1 },
        new Car { Id = 2, Make = "Volkswagen", Model = "Golf", Year = 2018, OwnerId = 1 },
        new Car { Id = 3, Make = "Toyota", Model = "Corolla", Year = 2020, OwnerId = 2 }
    };

    // Vrací všechny osoby vèetnì jejich aut
    public IEnumerable<Person> GetAll()
    {
        foreach (var person in _people)
        {
            person.Cars = _cars.Where(car => car.OwnerId == person.Id).ToList(); // Pøiøazení aut osobì podle OwnerId
        }
        return _people;
    }

    // Vyhledá osobu podle Id a vrátí ji vèetnì jejích aut
    public Person? GetById(int id)
    {
        var person = _people.FirstOrDefault(p => p.Id == id);
        if (person != null)
        {
            person.Cars = _cars.Where(car => car.OwnerId == person.Id).ToList(); // Pøiøazení aut osobì
        }
        return person;
    }

    // Pøidá novou osobu a její auta
    public Person AddPerson(Person person)
    {
        person.Id = _people.Max(p => p.Id) + 1; // Nastavení nového Id pro osobu

        foreach (var car in person.Cars)
        {
            car.Id = _cars.Max(c => c.Id) + 1; // Nastavení nového Id pro auto
            car.OwnerId = person.Id; // Nastavení OwnerId na Id osoby
            _cars.Add(car); // Pøidání auta do úložištì
        }

        _people.Add(person); // Pøidání osoby do úložištì
        return person;
    }
}

// GraphQL dotazovací tøída
public class Query
{
    private readonly PersonService _personService;

    public Query(PersonService personService)
    {
        _personService = personService;
    }

    [UseFiltering] // Umožòuje filtrování pøi dotazech
    [UseSorting]   // Umožòuje øazení výsledkù
    [GraphQLName("GetPeople")] // Specifikace jména v GraphQL schematu, bez nìho by název metody pro query zmìnil tak, aby zaèínalo malým písmenem
    public IEnumerable<Person> GetPeople() => _personService.GetAll(); // Vrací všechny osoby

    [GraphQLName("GetPersonById")] // Specifikace jména v GraphQL schematu, bez nìho by název metody pro query zmìnil tak, aby zaèínalo malým písmenem
    public Person? GetPersonById(int id) => _personService.GetById(id); // Vrací osobu podle Id
}

// Vstupní typ pro auto v mutaci
public record CarInput(string Make, string Model, int Year);

// GraphQL mutaèní tøída
public class Mutation
{
    private readonly PersonService _personService;

    public Mutation(PersonService personService)
    {
        _personService = personService;
    }

    // Pøidává novou osobu a její auta
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

// Hlavní program pro nastavení a spuštìní aplikace
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Registrace služby pro správu osob a aut
        builder.Services.AddSingleton<PersonService>();

        // Nastavení GraphQL serveru
        builder.Services
            .AddGraphQLServer()
            .AddQueryType<Query>() // Registrace dotazovací tøídy
            .AddMutationType<Mutation>() // Registrace mutaèní tøídy
            .AddFiltering() // Aktivace filtrování
            .AddSorting(); // Aktivace øazení

        var app = builder.Build();

        // Mapování GraphQL endpointu
        app.MapGraphQL();

        // Spuštìní aplikace
        app.Run();
    }
}

/*
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