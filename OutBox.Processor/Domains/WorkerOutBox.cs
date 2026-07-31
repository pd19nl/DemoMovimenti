using Ordini.Contracts;
using Ordini.Contracts.Models.OutBox;
using OutBox.Processor.Domains.Repositories.Dapper;
using RabbitMQ.Client;
using System.Text;

namespace OutBox.Processor.Domains;

/// <summary>
/// lettura periodica della tabella OutBox e trasmissione evento
/// </summary>
public class WorkerOutBox : BackgroundService
{
    private readonly ILogger<WorkerOutBox> _logger;
    private readonly IConfiguration _configuration;
    private readonly OutBoxRepositoryReader _OutBoxDB_Reader;
    private readonly OutBoxRepositoryCUD _OutBoxDB_CUD;
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
                        OutBoxRepositoryReader outBoxRepositoryReader,
                        OutBoxRepositoryCUD outBoxRepositoryCUD)
    {
        _logger = logger;
        _OutBoxDB_Reader = outBoxRepositoryReader;
        _rabbitConnection = rabbitConnection;
        _configuration = configuration;
        _OutBoxDB_CUD = outBoxRepositoryCUD;
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
        List<OutBoxMessage> messaggiDaElaborare = await _OutBoxDB_Reader.LetturaMessaggiDaElaborare();

        //pubblicazione dei messaggi in RabbitMQ
        using var channel = _rabbitConnection.CreateModel();

        channel.ExchangeDeclare(PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                                ExchangeType.Topic,
                                durable: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.DURABLE);

        foreach (OutBoxMessage m in messaggiDaElaborare)
        {
            try
            {
                string routingkey = PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.GetRoutingKeyForType(m.TipologiaEvento);

                var body = Encoding.UTF8.GetBytes(m.Payload);
                channel.BasicPublish(PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                                    routingkey,
                                    null,
                                    body);

                //processamento con successo del messaggio
                _OutBoxDB_CUD.UpdateProcessatoSuccess(m.Id.ToString());

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la pubblicazione del messaggio OutBox [{0}]", m.Id);
                _OutBoxDB_CUD.UpdateProcessatoError(m.Id.ToString(), ex.Message);
            }
        }
    }

    //chiusura worker: rilascio risorse
    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();

        base.Dispose();
    }




}
