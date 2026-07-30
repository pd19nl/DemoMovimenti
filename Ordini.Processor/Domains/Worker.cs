using Ordini.Contracts;
using Ordini.Processor.Domains.Repositories.Dapper;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

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

    //avvio worker - procedura di associazione alla coda RabbitMQ per i messaggi di interesse
    public override Task StartAsync(CancellationToken stoppingToken)
    {
        _channel = _rabbitConnection.CreateModel();

        _logger.LogInformation("DEFINIZIONE EXCHANGE {0}", PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini);
        //exchange degli eventi di tipo topic
        _channel.ExchangeDeclare(PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                                ExchangeType.Topic,
                                durable: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.DURABLE);

        //indicazione della coda specifica per il servizio di creazione ordini a partire da richiesta creazione ordini
        _logger.LogInformation("DEFINIZIONE CODA (QUEUE) {0}", PARAMETRI_GLOBALI.QUEUE.PROPRIETA.ORDINI_NAME);
        _channel.QueueDeclare(PARAMETRI_GLOBALI.QUEUE.PROPRIETA.ORDINI_NAME,
                              durable: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.DURABLE,
                              exclusive: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.ESCLUSIVE,
                              autoDelete: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.AUTODELETE);

        //sottoscrizione agli eventi
        //indicazione degli eventi ai quali si deve ricevere le notifiche
        //  caso richiesta creazione ordine
        _logger.LogInformation("SOTTOSCRIZIONE AD EVENTO {0}", PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.ORDINE.CREAZIONE);
        _channel.QueueBind(queue: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.ORDINI_NAME,
                           exchange: PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                           routingKey: PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.ORDINE.CREAZIONE);

        //  caso fallimento dalla saga da parte dell'inventario
        _logger.LogInformation("SOTTOSCRIZIONE AD EVENTO {0}", PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.INVENTARIO.NON_DISPONIBILE);
        _channel.QueueBind(queue: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.ORDINI_NAME,
                           exchange: PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                           routingKey: PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.INVENTARIO.NON_DISPONIBILE);

        //  caso fallimento dalla saga da parte del pagamento
        _logger.LogInformation("SOTTOSCRIZIONE AD EVENTO {0}", PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.PAGAMENTO.RESPINTO);
        _channel.QueueBind(queue: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.ORDINI_NAME,
                           exchange: PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                           routingKey: PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.PAGAMENTO.RESPINTO);

        // caso fine saga con successo
        _logger.LogInformation("SOTTOSCRIZIONE AD EVENTO {0}", PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.PAGAMENTO.EFFETTUATO);
        _channel.QueueBind(queue: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.ORDINI_NAME,
                           exchange: PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                           routingKey: PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.PAGAMENTO.EFFETTUATO);


        return base.StartAsync(stoppingToken);
    }



    //elaborazione dei messaggi
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += OnEventReceived;

        //Avvio consumo dei messaggi in coda;
        _channel.BasicConsume(queue: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.ORDINI_NAME,
                            autoAck: false,
                            consumer: consumer);

        //mantenimento del servizio in esecuzione
        await Task.Delay(Timeout.Infinite, stoppingToken);

        //while (!stoppingToken.IsCancellationRequested)
        //{
        //    if (_logger.IsEnabled(LogLevel.Information))
        //    {
        //        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        //    }
        //    await Task.Delay(1000, stoppingToken);
        //}
    }


    //gestore dei singoli eventi
    private async Task OnEventReceived(object sender, BasicDeliverEventArgs ea)
    {

    }




}
