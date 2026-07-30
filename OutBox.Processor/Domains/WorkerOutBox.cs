using Ordini.Contracts;
using Ordini.Contracts.Models.OutBox;
using OutBox.Processor.Domains.Repositories.Dapper;
using RabbitMQ.Client;

namespace OutBox.Processor.Domains;

/// <summary>
/// lettura periodica della tabella OutBox e trasmissione evento
/// </summary>
public class WorkerOutBox : BackgroundService
{
    private readonly ILogger<WorkerOutBox> _logger;
    private readonly IConfiguration _configuration;
    private readonly OutBoxRepositoryReader _dbOperation;
    private readonly IConnection _rabbitConnection;

    //periodo di ciclo di lettura 
    private readonly TimeSpan _periodoCiclo = TimeSpan.FromSeconds(3);

    private IModel? _channel;

    private static object semaforo = new object();
    private static bool workerInProgress = false;

    private static bool FlgProsegui()
    {
        bool ritorno = false;
        lock (semaforo)
        {
            if (!workerInProgress)
            {
                workerInProgress = true;
                ritorno = true;
            }
        }
        return ritorno;
    }

    public WorkerOutBox(ILogger<WorkerOutBox> logger,
                        IConfiguration configuration,
                        IConnection rabbitConnection,
                        OutBoxRepositoryReader outBoxRepositoryReader)
    {
        _logger = logger;
        _dbOperation = outBoxRepositoryReader;
        _rabbitConnection = rabbitConnection;
        _configuration = configuration;
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


    /// <summary>
    /// polling della tabella Outbox ad intervalli regolari
    /// </summary>
    /// <param name="stoppingToken"></param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //timer periodico
        using var timer = new PeriodicTimer(_periodoCiclo);

        // while (!stoppingToken.IsCancellationRequested)
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (!FlgProsegui())
                break;


            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }

            await ProcessaOutBoxMessagesAsync();

            workerInProgress = false;
            //await Task.Delay(3000, stoppingToken);
        }
    }



    /// <summary>
    /// lettura messaggi da elaborare
    /// </summary>
    /// <returns></returns>
    private async Task ProcessaOutBoxMessagesAsync()
    {
        //recupero dei messaggi dalla tabella
        List<OutBoxMessage> messaggiDaElaborare = await _dbOperation.LetturaMessaggiDaElaborare();

        //pubblicazione dei messaggi in Rabbit
    }


    //chiusura worker: rilascio risorse
    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();

        base.Dispose();
    }




}
