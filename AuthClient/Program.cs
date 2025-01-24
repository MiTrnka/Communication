//NuGet Swashbuckle.AspNetCore
/*
Otestov�n� pomoc� Swagger UI nebo PostMana:
Nejprve zavolejte /OAuthClient/get-refresh-token s u�ivatelsk�m jm�nem
Pot� zavolejte /OAuthClient/access-token s refresh tokenem
Nakonec zavolejte /OAuthClient/access-protected-resource s access tokenem
*/
/*
Klientsk� aplikace simuluje sc�n�� OAuth 2.0 komunikace:
Kl��ov� funkce:

Wrapper HTTP vol�n� na vzd�len� OAuth server
T�i z�kladn� endpointy:

Z�sk�n� refresh tokenu
V�m�na refresh tokenu za access token
P��stup k chr�n�n�mu zdroji s access tokenem



Principy implementace:

Vyu�it� HttpClient pro komunikaci
Manu�ln� serializace/deserializace token�
Odd�len� komunika�n�ch krok� pro lep�� pochopen� procesu
Bypass SSL validace pro lok�ln� v�voj
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