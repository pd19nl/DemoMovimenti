using Microsoft.AspNetCore.SignalR.Client;
using Notifiche.Processor;
using RabbitMQ.Client;
using Serilog;
using System.Reflection;


//serve ad inviare le notifiche al front-end

var builder = Host.CreateApplicationBuilder(args);


//  --- configurazione dei servizi ---
var configuration = builder.Configuration;

// =======================================================================================
//configurazione RabbitMQ
// come servizio singleton
builder.Services.AddSingleton(r =>
{
    //lettura configurazione
    var rabbitConfig = configuration.GetSection("RabbitMQ");

    //creazione factory
    var factory = new ConnectionFactory()
    {
        HostName = rabbitConfig["HostName"],
        UserName = rabbitConfig["UserName"],
        Password = rabbitConfig["Password"],
        Port = int.TryParse(rabbitConfig["Port"], out var port) ? port : 5672,
        VirtualHost = rabbitConfig["VirtualHost"] ?? "/",
        DispatchConsumersAsync = true //per i cunsumer asincroni
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
var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "Inventario.Worker";
builder.Logging.ClearProviders();

//lettura appsettings
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.WithProperty("Application", appName)
    .CreateLogger();

builder.Logging.AddSerilog();



// =======================================================================================
// Hub sul quale comunicare con API Interna dedicata ad Hub : Ordini.API
// registra una connessione singleton all'Hub della API
// il worker mantiene aperta la connessione (attiva) per inviar ele notifiche
builder.Services.AddSingleton<HubConnection>(sp =>
{
    //lettura urk
    var hubUrl = configuration["SignalR.HubUrl"] ?? throw new InvalidOperationException("Url Hub di SignalR non configurato");

    var connessione = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect() // tenta di riconnettersi 
});


// =======================================================================================
//registrazione del processo in background
builder.Services.AddHostedService<WorkerOrdine>();

var host = builder.Build();
host.Run();
