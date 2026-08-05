using Inventario.Processor.Domains;
using Inventario.Processor.Domains.Repositories.Dapper;
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
var appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "Inventario.Worker";
builder.Logging.ClearProviders();

//lettura appsettings
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.WithProperty("Application", appName)
    .CreateLogger();

builder.Logging.AddSerilog();


// =======================================================================================
//  aggiunta servizio di elaborazione ordini
builder.Services.AddScoped<GiacenzaRepositoryCRUD>();

// =======================================================================================
//registrazione del processo in background
builder.Services.AddHostedService<WorkerInventario_OrdineCreato>();
builder.Services.AddHostedService<WorkerInventario_PagamentoRespinto>();


// =======================================================================================
//
var host = builder.Build();


//AVVIO CON GESTIONE DELLE ECCEZIONI
//host.Run();
try
{
    Log.Information("Worker [{application}] avviato con successo",
        Assembly.GetEntryAssembly()?.GetName().Name);
    host.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex,
              "Worker [{application}] terminato in modo anomalo",
                Assembly.GetEntryAssembly()?.GetName().Name);
}
finally
{
    Log.CloseAndFlush();
}

