using Microsoft.AspNetCore.SignalR.Client;
using Ordini.Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Notifiche.Processor.Domains.Workers;

public class WorkerInventario : BackgroundService
{
    private readonly ILogger<WorkerOrdine> _logger;
    private readonly IConnection _rabbitConnection;
    private IModel? _channel;
    private readonly HubConnection _hubConnection;


    //elenco Exchange da gestire
    private readonly string _Queue_Exchange;

    //elenco queue da gestire
    private readonly string _Queue_Read_Inventario;

    //elenco routing da considerare nelle queue
    private readonly string _Queue_Read_Inventario_KeyRouting_Success;
    private readonly string _Queue_Read_Inventario_KeyRouting_Error;

    public WorkerInventario(ILogger<WorkerOrdine> logger,
                        IConnection rabbitConnection,
                        HubConnection hubConnection)
    {
        _logger = logger;
        _rabbitConnection = rabbitConnection;
        _hubConnection = hubConnection;


        //ASSEGNAZIONE EXCHANGE
        _Queue_Exchange = PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdini;

        //ASSEGNAZIONE QUEUE
        _Queue_Read_Inventario = PARAMETRI.QUEUE.PROPRIETA.PAGAMENTI.NAME;

        //ASSEGNAZIONE routing
        _Queue_Read_Inventario_KeyRouting_Success = PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.INVENTARIO.PROCESSATO.ALLOCATA;
        _Queue_Read_Inventario_KeyRouting_Error = PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.INVENTARIO.PROCESSATO.NON_DISPONIBILE;
    }


    //avvio worker - procedura di associazione alla coda RabbitMQ per i messaggi di interesse
    public override async Task StartAsync(CancellationToken stoppingToken)
    {
        _channel = _rabbitConnection.CreateModel();

        DichiarazioneExchange();

        DichiarazioneQueue();

        AssociazioneQueueESottoscrizioneExchange();

        await ConnessioneAdHubSignalR();

        await base.StartAsync(stoppingToken);
    }


    private async Task ConnessioneAdHubSignalR()
    {
        //l'hub è ospitato su Ordini.Api
        try
        {
            _logger.LogInformation("Tentativo di connessione  all'Hub SignalR ");
            await _hubConnection.StartAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Impossibile connettersi all'Hub SignalR aLL'avvio");
        }

    }

    private void DichiarazioneExchange()
    {
        _logger.LogInformation("DEFINIZIONE EXCHANGE {0}", _Queue_Exchange);
        //exchange degli eventi di tipo topic
        _channel.ExchangeDeclare(_Queue_Exchange,
                                ExchangeType.Topic,
                                durable: PARAMETRI.QUEUE.PROPRIETA.DURABLE);

    }


    private void DichiarazioneQueue()
    {
        DichiarazioneQueue_Read_From_Inventario();
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
                           arguments: null);

        _logger.LogInformation("ASSOCIAZIONE QUEUE {0} A EXCHANGE {1} e Sottoscrizione evento {2}",
                _Queue_Read_Inventario,
                _Queue_Exchange,
                _Queue_Read_Inventario_KeyRouting_Error);

        _channel.QueueBind(queue: _Queue_Read_Inventario,
                           exchange: _Queue_Exchange,
                           routingKey: _Queue_Read_Inventario_KeyRouting_Error,
                           arguments: null);

    }



    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        //ci si assicura che il canale sia pronto
        if (_channel == null)
        {
            _logger.LogError("Canale RabbitMQ non inizializzato. il Worker Inventario (Notifiche) non può avviarsi.");
            return;
        }

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
                case PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.INVENTARIO.PROCESSATO.ALLOCATA:
                    await Notifica_Inventario_Allocato(messaggio, pagamentoService);
                    break;

                case PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.INVENTARIO.PROCESSATO.NON_DISPONIBILE:
                    await Notifica_Inventario_NonDisponibile(messaggio, giacenzaRepositoryDB);
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


}
