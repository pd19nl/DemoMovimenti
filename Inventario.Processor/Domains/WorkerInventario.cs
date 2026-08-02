using Inventario.Processor.Domains.Repositories.Dapper;
using Ordini.Contracts;
using Ordini.Contracts.Events.Inventario;
using Ordini.Contracts.Events.Ordine;
using Ordini.Contracts.Events.Pagamento;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Inventario.Processor.Domains;

public class WorkerInventario : BackgroundService
{
    private readonly ILogger<WorkerInventario> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _rabbitConnection;
    private IModel? _channel;

    public WorkerInventario(ILogger<WorkerInventario> logger,
                            IServiceProvider serviceProvider,
                            IConnection connection)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _rabbitConnection = connection;
    }


    //avvio worker - procedura di associazione alla coda RabbitMQ per i messaggi di interesse
    public override Task StartAsync(CancellationToken stoppingToken)
    {
        _channel = _rabbitConnection.CreateModel();


        #region collegamento della coda principale alla DLX

        //setup che specifica la regola per indicare dove inviare i messaggi rifiutati
        var argumentsToDle = new Dictionary<string, object>
        {
            {"x-dead-letter-exchange",  PARAMETRI.QUEUE.PROPRIETA.ORDINI_NAME_DLQ}
        };
        #endregion

        DichiarazioneExchange();

        DichiarazioneQueue(argumentsToDle);

        AssociazioneQueuedESottoscrizioneExchange(argumentsToDle);

        return base.StartAsync(stoppingToken);
    }


    private void DichiarazioneExchange()
    {
        _logger.LogInformation("DEFINIZIONE EXCHANGE {0}", PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdini);
        //exchange degli eventi di tipo topic
        _channel.ExchangeDeclare(PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                                ExchangeType.Topic,
                                durable: PARAMETRI.QUEUE.PROPRIETA.DURABLE);

        //exchange Dead Letter (messaggi falliti) degli eventi di tipo topic
        _channel.ExchangeDeclare(PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdiniDle,
                                ExchangeType.Fanout,
                                durable: PARAMETRI.QUEUE.PROPRIETA.DURABLE);
    }

    private void DichiarazioneQueue(Dictionary<string, object> argumentsToDle)
    {

        //indicazione della coda specifica per il servizio di creazione ordini a partire da richiesta creazione ordini
        _logger.LogInformation("DEFINIZIONE CODA (QUEUE) {0}", PARAMETRI.QUEUE.PROPRIETA.ORDINI_NAME);
        _channel.QueueDeclare(queue: PARAMETRI.QUEUE.PROPRIETA.ORDINI_NAME,
                              durable: PARAMETRI.QUEUE.PROPRIETA.DURABLE,
                              exclusive: PARAMETRI.QUEUE.PROPRIETA.ESCLUSIVE,
                              autoDelete: PARAMETRI.QUEUE.PROPRIETA.AUTODELETE,
                              arguments: argumentsToDle);



        //indicazione della coda specifica per il servizio di creazione ordini a partire da richiesta creazione ordini
        _logger.LogInformation("DEFINIZIONE CODA DL (QUEUE) {0}", PARAMETRI.QUEUE.PROPRIETA.ORDINI_NAME);
        _channel.QueueDeclare(queue: PARAMETRI.QUEUE.PROPRIETA.ORDINI_NAME_DLQ,
                              durable: PARAMETRI.QUEUE.PROPRIETA.DURABLE,
                              exclusive: PARAMETRI.QUEUE.PROPRIETA.ESCLUSIVE,
                              autoDelete: PARAMETRI.QUEUE.PROPRIETA.AUTODELETE,
                              arguments: null);
    }

    private void AssociazioneQueuedESottoscrizioneExchange(Dictionary<string, object> argumentsToDle)
    {

        //sottoscrizione agli eventi
        //indicazione degli eventi ai quali si deve ricevere le notifiche
        //  caso richiesta creazione ordine
        //indicazione della regola per passare alla lista dle
        _logger.LogInformation("ASSOCIAZIONE QUEUE {0} A EXCHANGE {1} e Sottoscrizione evento {2}",
                                PARAMETRI.QUEUE.PROPRIETA.ORDINI_NAME,
                                PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                                PARAMETRI.QUEUE.KEY_EVENTO.ORDINE.PROCESSATO.CREATO);

        _channel.QueueBind(queue: PARAMETRI.QUEUE.PROPRIETA.ORDINI_NAME,
                           exchange: PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                           routingKey: PARAMETRI.QUEUE.KEY_EVENTO.ORDINE.PROCESSATO.CREATO,
                           arguments: argumentsToDle);

        //indicazione della regola per passare alla lista dle
        //  caso fallimento dalla saga da parte del pagamento
        //indicazione della regola per passare alla lista dle
        _logger.LogInformation("ASSOCIAZIONE QUEUE {0} A EXCHANGE {1} e Sottoscrizione evento {2}",
                                PARAMETRI.QUEUE.PROPRIETA.ORDINI_NAME,
                                PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                                PARAMETRI.QUEUE.KEY_EVENTO.PAGAMENTO.PROCESSATO.RESPINTO);

        _channel.QueueBind(queue: PARAMETRI.QUEUE.PROPRIETA.ORDINI_NAME,
                           exchange: PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                           routingKey: PARAMETRI.QUEUE.KEY_EVENTO.PAGAMENTO.PROCESSATO.RESPINTO,
                              arguments: argumentsToDle);



        _logger.LogInformation("ASSOCIAZIONE QUEUE {0} A EXCHANGE {1} e Sottoscrizione evento {2}",
                                PARAMETRI.QUEUE.PROPRIETA.ORDINI_NAME_DLQ,
                                PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdiniDle,
                                "");

        //collegamento Queue a Exchange
        _channel.QueueBind(
            queue: PARAMETRI.QUEUE.PROPRIETA.ORDINI_NAME_DLQ,
            exchange: PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdiniDle,
            routingKey: ""
            );

    }


    //elaborazione dei messaggi
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //configurazione consumer asincrono
        var consumer = new AsyncEventingBasicConsumer(_channel);
        //imposta il gestore dei messaggi ricevuti
        consumer.Received += OnEventReceived;

        //Avvio consumo dei messaggi in coda;
        _channel.BasicConsume(queue: PARAMETRI.QUEUE.PROPRIETA.ORDINI_NAME,
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
        //indica il tipo di evento
        string routingKey = ea.RoutingKey;

        //messaggio serializzato
        string messaggio = Encoding.UTF8.GetString(ea.Body.ToArray());

        _logger.LogInformation("Evento ricevuto con Routing Key: [{0}]", routingKey);

        try
        {
            //creazione dello scope
            using var scope = _serviceProvider.CreateScope();

            //istanziare un servizio DI
            var giacenzaRepositoryDB = scope.ServiceProvider.GetRequiredService<GiacenzaRepositoryCRUD>();

            switch (routingKey)
            {
                case PARAMETRI.QUEUE.KEY_EVENTO.ORDINE.PROCESSATO.CREATO:
                    await Gestione_Giacenza_Richiesta(messaggio, giacenzaRepositoryDB);
                    break;

                case PARAMETRI.QUEUE.KEY_EVENTO.PAGAMENTO.PROCESSATO.RESPINTO:
                    await Gestione_Giacenza_Ripristina(messaggio, giacenzaRepositoryDB);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Evento nella gestione evento con Routing Key: [{0}]", routingKey);

            //spostamente nella DLE Dead Letter Exchange
            _channel?.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    //da ordine creato si attiva la procedura di assegnazione giacenza
    private async Task Gestione_Giacenza_Richiesta(string messaggio, GiacenzaRepositoryCRUD servizioDB)
    {
        OrdineCreatoEvent evento = JsonSerializer.Deserialize<OrdineCreatoEvent>(messaggio);
        _logger.LogInformation("Fine processo di creazione, validazione ordine, inventario e pagamento ({0})", PARAMETRI.QUEUE.KEY_EVENTO.PAGAMENTO.PROCESSATO.EFFETTUATO);
        //evento.IdOrdine, evento.IdSaga,
        (bool esito, string? errore) = await servizioDB.ImpegnaScorte(evento);

        if (esito)
        {
            InventarioRiservatoEvent e = new InventarioRiservatoEvent
            {
                Ordine = evento
            };
            PubblicazioneEvento(PARAMETRI.QUEUE.KEY_EVENTO.INVENTARIO.PROCESSATO.ALLOCATA, e);

            _logger.LogInformation("Score riservato con successo per Ordine Id {0}", evento.IdOrdine);

        }
        else
        {
            InventarioNonDisponibileEvent e = new InventarioNonDisponibileEvent
            {
                IdOrdine = evento.IdOrdine,
                IdSaga = evento.IdSaga.ToString(),
                Motivo = errore
            };
            PubblicazioneEvento(PARAMETRI.QUEUE.KEY_EVENTO.INVENTARIO.PROCESSATO.NON_DISPONIBILE, e);

            _logger.LogInformation("Score non disponibili per Ordine Id {0}: Motivo [{1}]", evento.IdOrdine, errore);

        }

    }

    // a fronte di un fallito pagamento o di cancellazione ordine:
    private async Task Gestione_Giacenza_Ripristina(string messaggio, GiacenzaRepositoryCRUD servizioDB)
    {
        PagamentoFallitoEvent evento = JsonSerializer.Deserialize<PagamentoFallitoEvent>(messaggio);
        _logger.LogInformation("Ricevuto PagamentoFallitoEvent per ordine {0}. Avvio azione compensativa",
                               evento.IdOrdine);
        (bool esito, string? errore) = await servizioDB.LiberaScorte(evento);


    }

    private async Task PubblicazioneEvento<T>(string routing, T evento) where T : class
    {

    }

    //chiusura worker: rilascio risorse
    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();

        base.Dispose();
    }
}
