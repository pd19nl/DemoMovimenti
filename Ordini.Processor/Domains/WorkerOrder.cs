using Ordini.Contracts;
using Ordini.Contracts.Events.Inventario;
using Ordini.Contracts.Events.Ordine;
using Ordini.Contracts.Events.Pagamento;
using Ordini.Contracts.Models.Ordini;
using Ordini.Processor.Domains.Repositories.Dapper;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Ordini.Processor;

public class WorkerOrder : BackgroundService
{
    private readonly ILogger<WorkerOrder> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _rabbitConnection;
    private IModel? _channel;


    public WorkerOrder(ILogger<WorkerOrder> logger,
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
            {"x-dead-letter-exchange",  PARAMETRI.QUEUE.PROPRIETA.ORDINI.NAME_DLQ}
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

    }

    private void DichiarazioneQueue(Dictionary<string, object> argumentsToDle)
    {
        //nb non si associa la dle su questi eventi perchè non serve

        //indicazione della coda specifica per il servizio di creazione ordini a partire da richiesta creazione ordini
        _logger.LogInformation("DEFINIZIONE CODA (QUEUE) {0}", PARAMETRI.QUEUE.PROPRIETA.ORDINI.NAME);
        _channel.QueueDeclare(PARAMETRI.QUEUE.PROPRIETA.ORDINI.NAME,
                              durable: PARAMETRI.QUEUE.PROPRIETA.DURABLE,
                              exclusive: PARAMETRI.QUEUE.PROPRIETA.ESCLUSIVE,
                              autoDelete: PARAMETRI.QUEUE.PROPRIETA.AUTODELETE);
    }

    private void AssociazioneQueuedESottoscrizioneExchange(Dictionary<string, object> argumentsToDle)
    {
        //NB non si associa la dle perchè non serve su questi eventi


        //sottoscrizione agli eventi
        //indicazione degli eventi ai quali si deve ricevere le notifiche
        //  caso richiesta creazione ordine
        _logger.LogInformation("ASSOCIAZIONE QUEUE {0} A EXCHANGE {1} e Sottoscrizione evento {2}",
                                PARAMETRI.QUEUE.PROPRIETA.ORDINI.NAME,
                                PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                                PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.ORDINE.RICHIESTA.CREAZIONE);

        _channel.QueueBind(queue: PARAMETRI.QUEUE.PROPRIETA.ORDINI.NAME,
                           exchange: PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                           routingKey: PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.ORDINE.RICHIESTA.CREAZIONE);


        //  caso fallimento dalla saga da parte dell'inventario
        _logger.LogInformation("ASSOCIAZIONE QUEUE {0} A EXCHANGE {1} e Sottoscrizione evento {2}",
                                PARAMETRI.QUEUE.PROPRIETA.ORDINI.NAME,
                                PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                                PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.INVENTARIO.PROCESSATO.NON_DISPONIBILE);

        _channel.QueueBind(queue: PARAMETRI.QUEUE.PROPRIETA.ORDINI.NAME,
                           exchange: PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                           routingKey: PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.INVENTARIO.PROCESSATO.NON_DISPONIBILE);

        //  caso fallimento dalla saga da parte del pagamento
        _logger.LogInformation("ASSOCIAZIONE QUEUE {0} A EXCHANGE {1} e Sottoscrizione evento {2}",
                                PARAMETRI.QUEUE.PROPRIETA.ORDINI.NAME,
                                PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                                PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.PAGAMENTO.PROCESSATO.RESPINTO);

        _channel.QueueBind(queue: PARAMETRI.QUEUE.PROPRIETA.ORDINI.NAME,
                           exchange: PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                           routingKey: PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.PAGAMENTO.PROCESSATO.RESPINTO);

        // caso fine saga con successo
        _logger.LogInformation("ASSOCIAZIONE QUEUE {0} A EXCHANGE {1} e Sottoscrizione evento {2}",
                                PARAMETRI.QUEUE.PROPRIETA.ORDINI.NAME,
                                PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                                PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.PAGAMENTO.PROCESSATO.EFFETTUATO);

        _channel.QueueBind(queue: PARAMETRI.QUEUE.PROPRIETA.ORDINI.NAME,
                           exchange: PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                           routingKey: PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.PAGAMENTO.PROCESSATO.EFFETTUATO);

    }


    //elaborazione dei messaggi
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //ci si assicura che il canale sia pronto
        if (_channel == null)
        {
            _logger.LogError("Canale RabbitMQ non inizializzato. il Worker Ordine (Ordine) non può avviarsi.");
            return;
        }


        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += OnEventReceived;

        //Avvio consumo dei messaggi in coda;
        _channel.BasicConsume(queue: PARAMETRI.QUEUE.PROPRIETA.ORDINI.NAME,
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
            var ordineServiceDB = scope.ServiceProvider.GetRequiredService<OrdineRepositoryCRUD>();

            switch (routingKey)
            {
                case PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.ORDINE.RICHIESTA.CREAZIONE:
                    await Gestione_Ordine_Richiesta(messaggio, ordineServiceDB);
                    break;

                case PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.PAGAMENTO.PROCESSATO.EFFETTUATO:
                    // caso fine saga con successo
                    await Gestione_Ordine_Completato(messaggio, ordineServiceDB);
                    break;

                case PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.INVENTARIO.PROCESSATO.NON_DISPONIBILE:
                    await Gestione_Ordine_Inventario_NonDisponibile(messaggio, ordineServiceDB);
                    break;

                case PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.PAGAMENTO.PROCESSATO.RESPINTO:
                    await Gestione_Ordine_Pagamento_Respinto(messaggio, ordineServiceDB);
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


    private async Task Gestione_Ordine_Richiesta(string messaggio, OrdineRepositoryCRUD servizioDB)
    {
        _logger.LogInformation("Richiesta Creazione Ordine {0}", PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.ORDINE.RICHIESTA.CREAZIONE);
        OrdineRichiestoEvent eventoRichiesta = JsonSerializer.Deserialize<OrdineRichiestoEvent>(messaggio);

        var (nuovoId, messaggioOutbox) = await servizioDB.CreazioneOrderOutBoxAsync(eventoRichiesta);

        _logger.LogInformation("Operazione di creazione ordine completata con ID Ordine : [{0}] - Id Messaggio OutBox : [{0}]", nuovoId, messaggioOutbox.Id);
        _logger.LogInformation("Avvio SAGA per  ID Ordine : [{0}]", nuovoId);
    }


    private async Task Gestione_Ordine_Completato(string messaggio, OrdineRepositoryCRUD servizioDB)
    {
        PagamentoRiuscitoEvent evento = JsonSerializer.Deserialize<PagamentoRiuscitoEvent>(messaggio);
        _logger.LogInformation("Fine processo di creazione, validazione ordine, inventario e pagamento ({0})", PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.PAGAMENTO.PROCESSATO.EFFETTUATO);
        await servizioDB.UpdateStatoOrdineAsync(evento.IdOrdine, evento.IdSaga,
                                            eOrdineStato.OK_OrdineConcluso,
                                            "Saga completata con successo");


    }


    private async Task Gestione_Ordine_Inventario_NonDisponibile(string messaggio, OrdineRepositoryCRUD servizioDB)
    {
        InventarioNonDisponibileEvent evento = JsonSerializer.Deserialize<InventarioNonDisponibileEvent>(messaggio);
        //  caso fallimento dalla saga da parte dell'inventario
        _logger.LogInformation("Ordine annullato per Scorte non presenti ({0})", PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.INVENTARIO.PROCESSATO.NON_DISPONIBILE);
        await servizioDB.UpdateStatoOrdineAsync(evento.IdOrdine, evento.IdSaga,
                                            eOrdineStato.KO_ScorteNonPresenti,
                                            evento.Motivo);
    }


    private async Task Gestione_Ordine_Pagamento_Respinto(string messaggio, OrdineRepositoryCRUD servizioDB)
    {
        PagamentoFallitoEvent evento = JsonSerializer.Deserialize<PagamentoFallitoEvent>(messaggio);
        //  caso fallimento dalla saga da parte del pagamento
        _logger.LogInformation("Ordine annullato per Pagamento Rifiutato ({0})", PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.PAGAMENTO.PROCESSATO.RESPINTO);

        await servizioDB.UpdateStatoOrdineAsync(evento.IdOrdine, evento.IdSaga,
                                            eOrdineStato.KO_PagamentoFallito,
                                            evento.Motivo);
    }



    //chiusura worker: rilascio risorse
    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();

        base.Dispose();
    }


}
