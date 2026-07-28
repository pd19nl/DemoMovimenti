using FluentValidation;
using Ordini.Api.Configurations.JwtConfig;
using Ordini.Api.Configurations.SerilogConfig;
using Ordini.Api.Exceptions;
using Ordini.Api.Hubs;
using Ordini.Api.Repositories.Dapper;
using Ordini.Api.Validators.Ordine;
using RabbitMQ.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// =======================================================================================
//configurazione progetto
var configuration = builder.Configuration;


//aggiunta servizi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// =======================================================================================
//abilitazione JWT ed autenticazione
builder.Services.AddJwtAuthentication(configuration);
builder.Services.AddAuthorization();


// =======================================================================================
//servizi Scoped: per tutta la durata della richiesta HTTP, una istanza per richiesta HTTP
//lettura dati tramite datter -- CQRS
builder.Services.AddScoped<OrdineRepositoryReader>();



// =======================================================================================
//registrazione Validatori
builder.Services.AddValidatorsFromAssemblyContaining<AddOrdineValidator>();
//builder.Services.AddValidatorsFromAssemblyContaining<AddDettaglioOrdineValidator>();


// =======================================================================================
//configurazione RabbitMQ
// come servizio singleton
builder.Services.AddSingleton(r =>
{
    var factory = new ConnectionFactory()
    {
        HostName = configuration["RabbitMQ:HostName"],
        UserName = configuration["RabbitMQ:UserName"],
        Password = configuration["RabbitMQ:Password"],
    };

    return factory.CreateConnection();
});
//registrazione canale RabbitMQ come servizio Scoped
builder.Services.AddScoped(r =>
{
    var connection = r.GetRequiredService<IConnection>();
    return connection.CreateModel();
});


// =======================================================================================
// configurazione Serilog
builder.Services.AddHttpContextAccessor();
builder.Host.UseSerilog((context, service, logConfig) =>
{
    SerilogConfiguration.ConfigureSerilog(context, service, logConfig);
});



// =======================================================================================
// gestione globale delle eccezioni
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GloblalExceptionHandler>();



// =======================================================================================
//aggiunta supporto CORS per far connettere i client
//definizione di una polocy specifica
var WebAssClientPolicy = "WebAssClientPolicy";
builder.Services.AddCors(opt =>
{
    opt.AddPolicy(WebAssClientPolicy, policy =>
    {
        policy.WithOrigins("http://localhost:4200") //url del client
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials(); // ad uso del SignalR
    });
});



// =======================================================================================
// configurazione SignalR
builder.Services.AddSignalR();




// =======================================================================================
// parte nativa del progetto

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// disabilitato builder.Services.AddOpenApi();


// =======================================================================================
// configurazione pipeline

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // disabilitato app.MapOpenApi();

    app.UseSwagger();
    app.UseSwaggerUI();
}


// =======================================================================================
//applicazione policy CORS
app.UseCors(WebAssClientPolicy);



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


// =======================================================================================
//sicurezza
app.UseAuthentication(); //middlware di autenticazione
app.UseAuthorization(); //middlware di autorizzazione

// =======================================================================================
//configurazione SignalIR e mappatura Hub
app.MapHub<OrderStatusHub>("/orderStatusHub");




app.Run();
