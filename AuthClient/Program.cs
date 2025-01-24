//NuGet Swashbuckle.AspNetCore
/*
Otestování pomocí Swagger UI nebo PostMana:
Nejprve zavolejte /OAuthClient/get-refresh-token s uživatelským jménem
Poté zavolejte /OAuthClient/access-token s refresh tokenem
Nakonec zavolejte /OAuthClient/access-protected-resource s access tokenem
*/
/*
Klientská aplikace simuluje scénáø OAuth 2.0 komunikace:
Klíèové funkce:

Wrapper HTTP volání na vzdálený OAuth server
Tøi základní endpointy:

Získání refresh tokenu
Výmìna refresh tokenu za access token
Pøístup k chránìnému zdroji s access tokenem



Principy implementace:

Využití HttpClient pro komunikaci
Manuální serializace/deserializace tokenù
Oddìlení komunikaèních krokù pro lepší pochopení procesu
Bypass SSL validace pro lokální vývoj
*/
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();