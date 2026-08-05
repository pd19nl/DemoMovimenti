using Ordini.Contracts;
using Ordini.Contracts.Events.Inventario;
using Ordini.Contracts.Events.Pagamento;
using Pagamenti.Processor.Domains.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Pagamenti.Processor.Domains;

public class WorkerPagamenti_InventarioAllocato : BackgroundService
{
    private readonly ILogger<WorkerPagamenti_InventarioAllocato> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _rabbitConnection;
    private IModel? _channel;


    //elenco Exchange da gestire
    private readonly string _Queue_Exchange;
    private readonly string _Queue_Exchange_DLE;

    //elenco queue da gestire
    private readonly string _Queue_Read_Inventario;
    private readonly string _Queue_Read_Inventario_DLQ;
    private readonly string _Queue_Pubblicazione_Pagamento;

    //elenco routing da considerare nelle queue
    private readonly string _Queue_Read_Inventario_KeyRouting_Success;

    //collegamento della coda principale alla DLX
    Dictionary<string, object> argumentsToDle_Inventario = null;

    public WorkerPagamenti_InventarioAllocato(ILogger<WorkerPagamenti_InventarioAllocato> logger,
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
        _Queue_Read_Inventario = PARAMETRI.QUEUE.PROPRIETA.INVENTARIO.NAME;
        _Queue_Read_Inventario_DLQ = PARAMETRI.QUEUE.PROPRIETA.INVENTARIO.NAME_DLQ;
        _Queue_Pubblicazione_Pagamento = PARAMETRI.QUEUE.PROPRIETA.PAGAMENTI.NAME;

        //ASSEGNAZIONE routing
        _Queue_Read_Inventario_KeyRouting_Success = PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.INVENTARIO.PROCESSATO.ALLOCATA;
        //_Queue_Read_Inventario_KeyRouting_Error = PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.INVENTARIO.PROCESSATO.NON_DISPONIBILE;


        #region collegamento della coda principale alla DLX

        //setup che specifica la regola per indicare dove inviare i messaggi rifiutati
        argumentsToDle_Inventario = new Dictionary<string, object>
        {
            {"x-dead-letter-exchange",  _Queue_Read_Inventario_DLQ}
        };
        #endregion
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
        _logger.LogInformation("DEFINIZIONE EXCHANGE {0}", _Queue_Exchange);
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
        DichiarazioneQueue_Read_From_Inventario();

        DichiarazioneQueue_Pubblicazione_To_Pagamento();
    }

    private void DichiarazioneQueue_Read_From_Inventario()
    {

        //indicazione della coda di lettura eventi inventario
        _logger.LogInformation("DEFINIZIONE CODA (QUEUE) {0}",
                                _Queue_Read_Inventario);
        _channel.QueueDeclare(queue: _Queue_Read_Inventario,
                              durable: PARAMETRI.QUEUE.PROPRIETA.DURABLE,
                              exclusive: PARAMETRI.QUEUE.PROPRIETA.ESCLUSIVE,
                              autoDelete: PARAMETRI.QUEUE.PROPRIETA.AUTODELETE,
                              arguments: argumentsToDle_Inventario);



        //indicazione della coda DLQ per gli eventi inventario con anomalie di esecuzione
        _logger.LogInformation("DEFINIZIONE CODA DL (QUEUE) {0}",
                                _Queue_Read_Inventario_DLQ);
        _channel.QueueDeclare(queue: _Queue_Read_Inventario_DLQ,
                              durable: PARAMETRI.QUEUE.PROPRIETA.DURABLE,
                              exclusive: PARAMETRI.QUEUE.PROPRIETA.ESCLUSIVE,
                              autoDelete: PARAMETRI.QUEUE.PROPRIETA.AUTODELETE,
                              arguments: null);
    }

    private void DichiarazioneQueue_Pubblicazione_To_Pagamento()
    {

        //indicazione della coda DLQ per gli eventi inventario con anomalie di esecuzione
        _logger.LogInformation("DEFINIZIONE CODA DL (QUEUE) {0}",
                                _Queue_Pubblicazione_Pagamento);
        _channel.QueueDeclare(queue: _Queue_Pubblicazione_Pagamento,
                              durable: PARAMETRI.QUEUE.PROPRIETA.DURABLE,
                              exclusive: PARAMETRI.QUEUE.PROPRIETA.ESCLUSIVE,
                              autoDelete: PARAMETRI.QUEUE.PROPRIETA.AUTODELETE,
                              arguments: null);
    }


    private void AssociazioneQueueESottoscrizioneExchange()
    {
        _logger.LogInformation("ASSOCIAZIONE QUEUE {0} A EXCHANGE {1} e Sottoscrizione evento {2}",
                        _Queue_Read_Inventario,
                        _Queue_Exchange,
                        _Queue_Read_Inventario_KeyRouting_Success);

        _channel.QueueBind(queue: _Queue_Read_Inventario,
                           exchange: _Queue_Exchange,
                           routingKey: _Queue_Read_Inventario_KeyRouting_Success,
                           arguments: argumentsToDle_Inventario);

        //sottoscrizione agli eventi
        //indicazione degli eventi ai quali si deve ricevere le notifiche
        //  caso richiesta creazione ordine
        //indicazione della regola per passare alla lista dle
        //_logger.LogInformation("ASSOCIAZIONE QUEUE {0} A EXCHANGE {1} e Sottoscrizione evento {2}",
        //                        _Queue_Read_Inventario,
        //                        _Queue_Exchange,
        //                        _Queue_Read_Inventario_KeyRouting_Error);

        //_channel.QueueBind(queue: _Queue_Read_Inventario,
        //                   exchange: _Queue_Exchange,
        //                   routingKey: _Queue_Read_Inventario_KeyRouting_Error,
        //                   arguments: argumentsToDle_Inventario);





        _logger.LogInformation("ASSOCIAZIONE QUEUE {0} A EXCHANGE {1} e Sottoscrizione evento {2}",
                                _Queue_Read_Inventario_DLQ,
                                _Queue_Exchange_DLE,
                                "");

        //collegamento Queue a Exchange
        _channel.QueueBind(
            queue: _Queue_Read_Inventario_DLQ,
            exchange: _Queue_Exchange_DLE,
            routingKey: ""
            );




    }



    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        //configurazione consumer asincrono
        var consumer = new AsyncEventingBasicConsumer(_channel);
        //imposta il gestore dei messaggi ricevuti
        consumer.Received += OnEventReceived;

        //Avvio consumo dei messaggi in coda;
        _channel.BasicConsume(queue: _Queue_Read_Inventario,
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
            var pagamentoService = scope.ServiceProvider.GetRequiredService<PagamentoService>();

            switch (routingKey)
            {
                case PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.ORDINE.PROCESSATO.CREATO:
                    await Gestione_Pagamento_Effettua(messaggio, pagamentoService);
                    break;

                    //case PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.PAGAMENTO.PROCESSATO.RESPINTO:
                    //    await Gestione_Giacenza_Ripristina(messaggio, giacenzaRepositoryDB);
                    //    break;
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
    private async Task Gestione_Pagamento_Effettua(string messaggio, PagamentoService servizioPagamento)
    {
        InventarioRiservatoEvent evento = JsonSerializer.Deserialize<InventarioRiservatoEvent>(messaggio);
        if (evento == null)
        {
            throw new JsonException("Impossibile deserializzare InventarioRiservatoEvent");
        }

        _logger.LogInformation("Fine processo di creazione, validazione ordine, inventario e pagamento ({0})", PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.PAGAMENTO.PROCESSATO.EFFETTUATO);
        //evento.IdOrdine, evento.IdSaga,
        (bool esito, string? errore) = await servizioPagamento.EffettuaPagamento(evento);

        if (esito)
        {
            PagamentoRiuscitoEvent e = new PagamentoRiuscitoEvent
            {
                IdOrdine = evento.Ordine.IdOrdine,
                IdSaga = evento.Ordine.IdSaga.ToString()
            };
            PubblicazioneEvento(PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.PAGAMENTO.PROCESSATO.EFFETTUATO, e);

            _logger.LogInformation("Pagamento riuscito per Ordine Id {0}", evento.Ordine.IdOrdine);

        }
        else
        {
            PagamentoFallitoEvent e = new PagamentoFallitoEvent
            {
                IdOrdine = evento.Ordine.IdOrdine,
                IdSaga = evento.Ordine.IdSaga.ToString(),
                Motivo = errore,
                Prodotti = evento.Ordine.Prodotti
            };
            await PubblicazioneEvento(PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.PAGAMENTO.PROCESSATO.RESPINTO, e);

            _logger.LogInformation("Pagamento fallito per Ordine Id {0}: Motivo [{1}]", evento.Ordine.IdOrdine, errore);

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

}
