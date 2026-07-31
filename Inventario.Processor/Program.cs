using Inventario.Processor.Domains;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<WorkerInventario>();

var host = builder.Build();
host.Run();
