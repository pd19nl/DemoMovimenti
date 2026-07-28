using Ordini.Api.Configurations.JwtConfig;
using Ordini.Api.Repositories.Dapper;

var builder = WebApplication.CreateBuilder(args);

//configurazione progetto
var configuration = builder.Configuration;


//aggiunta servizi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


//abilitazione JWT ed autenticazione
builder.Services.AddJwtAuthentication(configuration);
builder.Services.AddAuthorization();

//servizi Scoped: per tutta la durata della richiesta HTTP
builder.Services.AddScoped<OrdineRepositoryReader>();









// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

//app.MapGet("/weatherforecast", () =>
//{
//    var forecast =  Enumerable.Range(1, 5).Select(index =>
//        new WeatherForecast
//        (
//            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//            Random.Shared.Next(-20, 55),
//            summaries[Random.Shared.Next(summaries.Length)]
//        ))
//        .ToArray();
//    return forecast;
//})
//.WithName("GetWeatherForecast");

app.Run();
