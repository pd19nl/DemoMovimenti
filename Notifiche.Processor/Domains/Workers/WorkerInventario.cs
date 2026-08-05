using Microsoft.AspNetCore.SignalR.Client;
using Ordini.ApplicationAPI.Models.DTOs.Ordine.Notifiche;
using Ordini.Contracts;
using Ordini.Contracts.Events.Inventario;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

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
        //autoack indica la conferma automatica dei messaggi.
        //Quando è attivo su true, il server rimuove il messaggio dalla coda non appena lo
        //invia al client, senza attendere che il codice C# confermi l'avvenuta elaborazione
        _channel.BasicConsume(queue: _Queue_Read_Inventario,
                            autoAck: true,
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
            switch (routingKey)
            {
                case PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.INVENTARIO.PROCESSATO.ALLOCATA:
                    await Notifica_Inventario_Allocato(messaggio);
                    break;

                case PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.INVENTARIO.PROCESSATO.NON_DISPONIBILE:
                    await Notifica_Inventario_NonDisponibile(messaggio);
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



    private async Task Notifica_Inventario_Allocato(string messaggio)
    {
        InventarioNonDisponibileEvent evento = JsonSerializer.Deserialize<InventarioNonDisponibileEvent>(messaggio);
        if (evento == null)
        {
            throw new JsonException("Impossibile deserializzare InventarioNonDisponibileEvent");
        }

        //se non si è connessi all'hub non si possono inviare notifiche
        //il messaggio viene scartato per via di autoAck=true
        //con autoAck a false, gestire eventuale DLE
        if (_hubConnection.State != HubConnectionState.Connected)
        {
            _logger.LogError("Ricevuto evento RabbitMQ per ordine {0} tipo Ordine Pagato, ma non connesso all'Hub SignalR. Messaggio scartato", evento.IdOrdine);
        }

        eOrdineStatus status = eOrdineStatus.InElaborazione;
        string motivo = $"Merce allocata";
        string ordineId = evento.IdOrdine;

        InviaNotifica(status, motivo, ordineId);

    }
    private async Task Notifica_Inventario_NonDisponibile(string messaggio)
    {
        InventarioNonDisponibileEvent evento = JsonSerializer.Deserialize<InventarioNonDisponibileEvent>(messaggio);
        if (evento == null)
        {
            throw new JsonException("Impossibile deserializzare InventarioNonDisponibileEvent");
        }

        //se non si è connessi all'hub non si possono inviare notifiche
        //il messaggio viene scartato per via di autoAck=true
        //con autoAck a false, gestire eventuale DLE
        if (_hubConnection.State != HubConnectionState.Connected)
        {
            _logger.LogError("Ricevuto evento RabbitMQ per ordine {0} tipo Ordine Pagato, ma non connesso all'Hub SignalR. Messaggio scartato", evento.IdOrdine);
        }

        eOrdineStatus status = eOrdineStatus.NonAccettato;
        string motivo = $"Disponibilità merce non presente: {evento.Motivo}";
        string ordineId = evento.IdOrdine;

        InviaNotifica(status, motivo, ordineId);

    }

    private async Task InviaNotifica(eOrdineStatus stato, string motivo, string idOrdine)
    {
        try
        {
            await _hubConnection.InvokeAsync("SendNotiticationToGroup",
                                                idOrdine,
                                                stato,
                                                motivo);
            _logger.LogInformation($"Notifica per IdOrdine {idOrdine} inviata all'HUB");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante l'invio della notifica all'Hub SignalR");
        }
    }



    //chiusura worker: rilascio risorse
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _channel?.Close();
        _channel?.Dispose();

        if (_hubConnection != null)
        {
            await _hubConnection.DisposeAsync();
        }

        base.Dispose();
    }

}
