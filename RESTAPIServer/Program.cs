var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
var app = builder.Build();

app.MapGet("/", () => "API bezi na /API/cats");
app.MapControllers();
app.Run();
