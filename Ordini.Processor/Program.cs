using Ordini.Processor;
using Ordini.Processor.Domains.Repositories.Dapper;
using RabbitMQ.Client;
using Serilog;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);

//  --- configurazione dei servizi ---
var configuration = builder.Configuration;

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
        DispatchConsumersAsync = true
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
var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "Ordini.Worker";
builder.Logging.ClearProviders();

//lettura appsettings
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.WithProperty("Application", appName)
    .CreateLogger();

builder.Logging.AddSerilog();


// =======================================================================================
//  aggiunta servizio di elaborazione ordini
builder.Services.AddScoped<OrdineRepositoryReader>();

// =======================================================================================
//registrazione del processo in background
builder.Services.AddHostedService<WorkerOrder>();



// =======================================================================================
//
var host = builder.Build();
host.Run();
