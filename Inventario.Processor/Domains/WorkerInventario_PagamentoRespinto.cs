using Inventario.Processor.Domains.Repositories.Dapper;
using Ordini.Contracts;
using Ordini.Contracts.Events.Inventario;
using Ordini.Contracts.Events.Pagamento;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Inventario.Processor.Domains;

public class WorkerInventario_PagamentoRespinto : BackgroundService
{
    private readonly ILogger<WorkerInventario_PagamentoRespinto> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _rabbitConnection;
    private IModel? _channel;

    //elenco Exchange da gestire
    private readonly string _Queue_Exchange;
    private readonly string _Queue_Exchange_DLE;

    //elenco queue da gestire        
    private readonly string _Queue_Read_Pagamento;
    private readonly string _Queue_Read_Pagamento_DLQ;
    private readonly string _Queue_Pubblicazione_Inventario;

    //argomento di indirizzamento DLQ        
    Dictionary<string, object> argumentsToDle_Pagamento;

    //elenco routing da considerare nelle queue        
    private readonly string _Queue_Read_Pagamento_KeyRouting_Error;
    private readonly string _Queue_Pubblicazione_Inventario_KeyRouting_Ripristinato;


    public WorkerInventario_PagamentoRespinto(ILogger<WorkerInventario_PagamentoRespinto> logger,
                            IServiceProvider serviceProvider,
                            IConnection connection)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _rabbitConnection = connection;

        //ASSEGNAZIONE EXCHANGE
        _Queue_Exchange = PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdini;
        _Queue_Exchange_DLE = PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdiniDle;

        //ASSEGNAZIONE QUEUE
        _Queue_Read_Pagamento = PARAMETRI.QUEUE.PROPRIETA.PAGAMENTI.NAME;
        _Queue_Read_Pagamento_DLQ = PARAMETRI.QUEUE.PROPRIETA.PAGAMENTI.NAME_DLQ;

        _Queue_Pubblicazione_Inventario = PARAMETRI.QUEUE.PROPRIETA.INVENTARIO.NAME;


        #region collegamento della coda principale alla DLX

        //setup che specifica la regola per indicare dove inviare i messaggi rifiutati
        //LA DLQ è RELATIVA AGLI EVENTI ORDINI DI CUI LEGGE            
        argumentsToDle_Pagamento = new Dictionary<string, object>
        {
            {"x-dead-letter-exchange", _Queue_Read_Pagamento_DLQ}
        };
        #endregion

        //routing delle queue        
        _Queue_Read_Pagamento_KeyRouting_Error = PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.PAGAMENTO.PROCESSATO.RESPINTO;
        _Queue_Pubblicazione_Inventario_KeyRouting_Ripristinato = PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.INVENTARIO.PROCESSATO.RIALLOCATA;
    }


    //avvio worker - procedura di associazione alla coda RabbitMQ per i messaggi di interesse
    public override Task StartAsync(CancellationToken stoppingToken)
    {
        _channel = _rabbitConnection.CreateModel();

        DichiarazioneExchange();

        DichiarazioneQueue();

        AssociazioneQueueESottoscrizioneExchange();

        return base.StartAsync(stoppingToken);
    }


    private void DichiarazioneExchange()
    {
        _logger.LogInformation("DEFINIZIONE EXCHANGE {0}", PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdini);
        //exchange degli eventi di tipo topic
        _channel.ExchangeDeclare(_Queue_Exchange,
                                ExchangeType.Topic,
                                durable: PARAMETRI.QUEUE.PROPRIETA.DURABLE);

        //exchange Dead Letter (messaggi falliti) degli eventi di tipo topic
        _channel.ExchangeDeclare(_Queue_Exchange_DLE,
                                ExchangeType.Fanout,
                                durable: PARAMETRI.QUEUE.PROPRIETA.DURABLE);
    }

    private void DichiarazioneQueue()
    {
        DichiarazioneQueue_Read_From_Pagamenti();

        DichiarazioneQueue_Pubblicazione_To_Inventario();
    }


    private void DichiarazioneQueue_Read_From_Pagamenti()
    {

        #region AMBITO PAGAMENTI
        //LETTURA EVENTI DA ORDINE
        //indicazione della coda specifica per il servizio di creazione ordini a partire da richiesta creazione ordini
        _logger.LogInformation("DEFINIZIONE CODA (QUEUE) {0}",
                                _Queue_Read_Pagamento);
        _channel.QueueDeclare(queue: _Queue_Read_Pagamento,
                              durable: PARAMETRI.QUEUE.PROPRIETA.DURABLE,
                              exclusive: PARAMETRI.QUEUE.PROPRIETA.ESCLUSIVE,
                              autoDelete: PARAMETRI.QUEUE.PROPRIETA.AUTODELETE,
                              arguments: argumentsToDle_Pagamento);


        //SPOSTAMENTI EVENTI IN DLQ
        //indicazione della coda specifica per il servizio di creazione ordini a partire da richiesta creazione ordini
        _logger.LogInformation("DEFINIZIONE CODA DL (QUEUE) {0}",
                                _Queue_Read_Pagamento_DLQ);
        _channel.QueueDeclare(queue: _Queue_Read_Pagamento_DLQ,
                              durable: PARAMETRI.QUEUE.PROPRIETA.DURABLE,
                              exclusive: PARAMETRI.QUEUE.PROPRIETA.ESCLUSIVE,
                              autoDelete: PARAMETRI.QUEUE.PROPRIETA.AUTODELETE,
                              arguments: null);
        #endregion

    }

    private void DichiarazioneQueue_Pubblicazione_To_Inventario()
    {
        #region PUBBLICAZIONE INVENTARIO
        //PUBBLICAZIONE EVENTI
        _logger.LogInformation("DEFINIZIONE CODA (QUEUE) {0}",
                                _Queue_Pubblicazione_Inventario);
        _channel.QueueDeclare(queue: _Queue_Pubblicazione_Inventario,
                              durable: PARAMETRI.QUEUE.PROPRIETA.DURABLE,
                              exclusive: PARAMETRI.QUEUE.PROPRIETA.ESCLUSIVE,
                              autoDelete: PARAMETRI.QUEUE.PROPRIETA.AUTODELETE,
                              arguments: null);
        #endregion
    }

    private void AssociazioneQueueESottoscrizioneExchange()
    {
        //LETTURA EVENTI 
        //sottoscrizione agli eventi
        //indicazione degli eventi ai quali si deve ricevere le notifiche
        //  caso richiesta creazione ordine
        //indicazione della regola per passare alla lista dle
        _logger.LogInformation("ASSOCIAZIONE QUEUE {0} A EXCHANGE {1} e Sottoscrizione evento {2}",
                                _Queue_Read_Pagamento,
                                _Queue_Exchange,
                                _Queue_Read_Pagamento_KeyRouting_Error);

        _channel.QueueBind(queue: _Queue_Read_Pagamento,
                           exchange: _Queue_Exchange,
                           routingKey: _Queue_Read_Pagamento_KeyRouting_Error,
                           arguments: argumentsToDle_Pagamento);



        //PUBBLICAZIONE EVENTI
        //indicazione della regola per passare alla lista dle
        //  caso fallimento dalla saga da parte del pagamento
        //indicazione della regola per passare alla lista dle
        _logger.LogInformation("ASSOCIAZIONE QUEUE {0} A EXCHANGE {1} e Sottoscrizione evento {2}",
                                _Queue_Pubblicazione_Inventario,
                                _Queue_Exchange,
                                _Queue_Pubblicazione_Inventario_KeyRouting_Ripristinato);

        _channel.QueueBind(queue: _Queue_Pubblicazione_Inventario,
                           exchange: _Queue_Exchange,
                           routingKey: _Queue_Pubblicazione_Inventario_KeyRouting_Ripristinato,
                              arguments: null);



        _logger.LogInformation("ASSOCIAZIONE QUEUE {0} A EXCHANGE {1} e Sottoscrizione evento {2}",
                                _Queue_Read_Pagamento_DLQ,
                                _Queue_Exchange_DLE,
                                "");

        //collegamento Queue a Exchange
        _channel.QueueBind(
            queue: PARAMETRI.QUEUE.PROPRIETA.ORDINI.NAME_DLQ,
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
        _channel.BasicConsume(queue: _Queue_Read_Pagamento,
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
                //case PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.ORDINE.PROCESSATO.CREATO:
                //    await Gestione_Giacenza_Richiesta(messaggio, giacenzaRepositoryDB);
                //    break;

                case PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.PAGAMENTO.PROCESSATO.RESPINTO:
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
    //private async Task Gestione_Giacenza_Richiesta(string messaggio, GiacenzaRepositoryCRUD servizioDB)
    //{
    //    OrdineCreatoEvent evento = JsonSerializer.Deserialize<OrdineCreatoEvent>(messaggio);
    //    _logger.LogInformation("Fine processo di creazione, validazione ordine, inventario e pagamento ({0})", PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.PAGAMENTO.PROCESSATO.EFFETTUATO);
    //    //evento.IdOrdine, evento.IdSaga,
    //    (bool esito, string? errore) = await servizioDB.ImpegnaScorte(evento);

    //    if (esito)
    //    {
    //        InventarioRiservatoEvent e = new InventarioRiservatoEvent
    //        {
    //            Ordine = evento
    //        };
    //        PubblicazioneEvento(PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.INVENTARIO.PROCESSATO.ALLOCATA, e);

    //        _logger.LogInformation("Score riservato con successo per Ordine Id {0}", evento.IdOrdine);

    //    }
    //    else
    //    {
    //        InventarioNonDisponibileEvent e = new InventarioNonDisponibileEvent
    //        {
    //            IdOrdine = evento.IdOrdine,
    //            IdSaga = evento.IdSaga.ToString(),
    //            Motivo = errore
    //        };
    //        await PubblicazioneEvento(PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.INVENTARIO.PROCESSATO.NON_DISPONIBILE, e);

    //        _logger.LogInformation("Score non disponibili per Ordine Id {0}: Motivo [{1}]", evento.IdOrdine, errore);

    //    }

    //}

    // a fronte di un fallito pagamento o di cancellazione ordine:
    private async Task Gestione_Giacenza_Ripristina(string messaggio, GiacenzaRepositoryCRUD servizioDB)
    {
        PagamentoFallitoEvent evento = JsonSerializer.Deserialize<PagamentoFallitoEvent>(messaggio);
        _logger.LogInformation("Ricevuto PagamentoFallitoEvent per ordine {0}. Avvio azione compensativa",
                               evento.IdOrdine);
        (bool esito, string? errore) = await servizioDB.LiberaScorte(evento);

        if (esito)
        {
            InventarioRipristinatoEvent eventoCompensazione = new InventarioRipristinatoEvent
            {
                IdOrdine = evento.IdOrdine,
                IdSaga = evento.IdSaga.ToString(),
            };
            await PubblicazioneEvento(_Queue_Pubblicazione_Inventario_KeyRouting_Ripristinato,
                                        eventoCompensazione);

        }
    }

    private async Task PubblicazioneEvento(string routing, Object evento)
    {
        string messaggioBody = JsonSerializer.Serialize(evento);
        var body = Encoding.UTF8.GetBytes(messaggioBody);
        _channel.BasicPublish(_Queue_Exchange,
                                routing,
                                null, body);
    }

    //chiusura worker: rilascio risorse
    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();

        base.Dispose();
    }
}
