using Notifiche.Processor;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<WorkerOrdine>();

var host = builder.Build();
host.Run();
