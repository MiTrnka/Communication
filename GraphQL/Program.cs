namespace GraphQL;
//Doinstaloval jsem si NuGet bal��ky HotChocolate.AspNetCore a HotChocolate.Data

// T��da reprezentuj�c� osobu
public class Person
{
    public int Id { get; set; } // Jedine�n� identifik�tor osoby
    public string FirstName { get; set; } // K�estn� jm�no
    public string LastName { get; set; } // P��jmen�
    public int Age { get; set; } // V�k
    public List<Car> Cars { get; set; } = new(); // Seznam aut vlastn�n�ch osobou
}

// T��da reprezentuj�c� auto
public class Car
{
    public int Id { get; set; } // Jedine�n� identifik�tor auta
    public string Make { get; set; } // Zna�ka auta
    public string Model { get; set; } // Model auta
    public int Year { get; set; } // Rok v�roby
    public int OwnerId { get; set; } // Id vlastn�ka (osoby)
}

// Slu�ba pro pr�ci s daty osob a aut, simuluje datab�zi
public class PersonService
{
    // Intern� �lo�i�t� osob
    private readonly List<Person> _people = new()
    {
        new Person { Id = 1, FirstName = "Jan", LastName = "Nov�k", Age = 30 },
        new Person { Id = 2, FirstName = "Petra", LastName = "Svobodov�", Age = 25 }
    };

    // Intern� �lo�i�t� aut
    private readonly List<Car> _cars = new()
    {
        new Car { Id = 1, Make = "�koda", Model = "Octavia", Year = 2015, OwnerId = 1 },
        new Car { Id = 2, Make = "Volkswagen", Model = "Golf", Year = 2018, OwnerId = 1 },
        new Car { Id = 3, Make = "Toyota", Model = "Corolla", Year = 2020, OwnerId = 2 }
    };

    // Vrac� v�echny osoby v�etn� jejich aut
    public IEnumerable<Person> GetAll()
    {
        foreach (var person in _people)
        {
            person.Cars = _cars.Where(car => car.OwnerId == person.Id).ToList(); // P�i�azen� aut osob� podle OwnerId
        }
        return _people;
    }

    // Vyhled� osobu podle Id a vr�t� ji v�etn� jej�ch aut
    public Person? GetById(int id)
    {
        var person = _people.FirstOrDefault(p => p.Id == id);
        if (person != null)
        {
            person.Cars = _cars.Where(car => car.OwnerId == person.Id).ToList(); // P�i�azen� aut osob�
        }
        return person;
    }

    // P�id� novou osobu a jej� auta
    public Person AddPerson(Person person)
    {
        person.Id = _people.Max(p => p.Id) + 1; // Nastaven� nov�ho Id pro osobu

        foreach (var car in person.Cars)
        {
            car.Id = _cars.Max(c => c.Id) + 1; // Nastaven� nov�ho Id pro auto
            car.OwnerId = person.Id; // Nastaven� OwnerId na Id osoby
            _cars.Add(car); // P�id�n� auta do �lo�i�t�
        }

        _people.Add(person); // P�id�n� osoby do �lo�i�t�
        return person;
    }
}

// GraphQL dotazovac� t��da
public class Query
{
    private readonly PersonService _personService;

    public Query(PersonService personService)
    {
        _personService = personService;
    }

    [UseFiltering] // Umo��uje filtrov�n� p�i dotazech
    [UseSorting]   // Umo��uje �azen� v�sledk�
    [GraphQLName("GetPeople")] // Specifikace jm�na v GraphQL schematu, bez n�ho by n�zev metody pro query zm�nil tak, aby za��nalo mal�m p�smenem
    public IEnumerable<Person> GetPeople() => _personService.GetAll(); // Vrac� v�echny osoby

    [GraphQLName("GetPersonById")] // Specifikace jm�na v GraphQL schematu, bez n�ho by n�zev metody pro query zm�nil tak, aby za��nalo mal�m p�smenem
    public Person? GetPersonById(int id) => _personService.GetById(id); // Vrac� osobu podle Id
}

// Vstupn� typ pro auto v mutaci
public record CarInput(string Make, string Model, int Year);

// GraphQL muta�n� t��da
public class Mutation
{
    private readonly PersonService _personService;

    public Mutation(PersonService personService)
    {
        _personService = personService;
    }

    // P�id�v� novou osobu a jej� auta
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

// Hlavn� program pro nastaven� a spu�t�n� aplikace
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Registrace slu�by pro spr�vu osob a aut
        builder.Services.AddSingleton<PersonService>();

        // Nastaven� GraphQL serveru
        builder.Services
            .AddGraphQLServer()
            .AddQueryType<Query>() // Registrace dotazovac� t��dy
            .AddMutationType<Mutation>() // Registrace muta�n� t��dy
            .AddFiltering() // Aktivace filtrov�n�
            .AddSorting(); // Aktivace �azen�

        var app = builder.Build();

        // Mapov�n� GraphQL endpointu
        app.MapGraphQL();

        // Spu�t�n� aplikace
        app.Run();
    }
}

/*
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