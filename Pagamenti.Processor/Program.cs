using Pagamenti.Processor.Domains;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<WorkerPagamenti>();

var host = builder.Build();
host.Run();
