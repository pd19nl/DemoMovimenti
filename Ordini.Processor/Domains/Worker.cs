using Ordini.Contracts;
using Ordini.Processor.Domains.Repositories.Dapper;
using RabbitMQ.Client;

namespace Ordini.Processor;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly OrdineRepositoryReader _dbOperation;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _rabbitConnection;
    private IModel? _channel;


    public Worker(ILogger<Worker> logger,
        IServiceProvider serviceProvider,
        OrdineRepositoryReader ordineRepositoryReader,
        IConnection connection
        )
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _dbOperation = ordineRepositoryReader;
        _rabbitConnection = connection;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _rabbitConnection.CreateModel();

        //exchange degli eventi di tipo topic
        _channel.ExchangeDeclare(PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                                ExchangeType.Topic,
                                durable: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.DURABLE);

        //indicazione della coda specifica per il servizio di creazione ordini a partire da richiesta creazione ordini
        _channel.QueueDeclare(PARAMETRI_GLOBALI.QUEUE.PROPRIETA.ORDINI_NAME,
                              durable: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.DURABLE,
                              exclusive: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.ESCLUSIVE,
                              autoDelete: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.AUTODELETE);

        //sottoscrizione agli eventi
        //indicazione degli eventi ai quali si deve ricevere le notifiche
        //  caso richiesta creazione ordine
        _channel.QueueBind(queue: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.ORDINI_NAME,
                           exchange: PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                           routingKey: PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.ORDINE.CREAZIONE);

        //  caso fallimento dalla saga da parte dell'inventario
        _channel.QueueBind(queue: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.ORDINI_NAME,
                           exchange: PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                           routingKey: PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.INVENTARIO.NON_DISPONIBILE);

        //  caso fallimento dalla saga da parte del pagamento
        _channel.QueueBind(queue: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.ORDINI_NAME,
                           exchange: PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                           routingKey: PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.PAGAMENTO.RESPINTO);
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
