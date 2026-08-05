using Microsoft.AspNetCore.SignalR.Client;
using Ordini.Contracts;
using RabbitMQ.Client;

namespace Notifiche.Processor.Domains.Workers;

public class WorkerPagamento : BackgroundService
{
    private readonly ILogger<WorkerOrdine> _logger;
    private readonly IConnection _rabbitConnection;
    private IModel? _channel;
    private readonly HubConnection _hubConnection;


    //elenco Exchange da gestire
    private readonly string _Queue_Exchange;

    //elenco queue da gestire
    private readonly string _Queue_Read_Pagamento;

    //elenco routing da considerare nelle queue
    private readonly string _Queue_Read_Pagamento_KeyRouting_Success;
    private readonly string _Queue_Read_Pagamento_KeyRouting_Error;

    public WorkerPagamento(ILogger<WorkerOrdine> logger,
                        IConnection rabbitConnection,
                        HubConnection hubConnection)
    {
        _logger = logger;
        _rabbitConnection = rabbitConnection;
        _hubConnection = hubConnection;


        //ASSEGNAZIONE EXCHANGE
        _Queue_Exchange = PARAMETRI.QUEUE.EXCHANGE.NomeExchangeOrdini;

        //ASSEGNAZIONE QUEUE
        _Queue_Read_Pagamento = PARAMETRI.QUEUE.PROPRIETA.PAGAMENTI.NAME;

        //ASSEGNAZIONE routing
        _Queue_Read_Pagamento_KeyRouting_Success = PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.PAGAMENTO.PROCESSATO.EFFETTUATO;
        _Queue_Read_Pagamento_KeyRouting_Error = PARAMETRI.QUEUE.KEY_ROUTING_EVENTO.PAGAMENTO.PROCESSATO.RESPINTO;
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

    }


    private void DichiarazioneQueue()
    {
        DichiarazioneQueue_Read_From_Pagamenti();
    }

    private void DichiarazioneQueue_Read_From_Pagamenti()
    {

        //indicazione della coda di lettura eventi inventario
        _logger.LogInformation("DEFINIZIONE CODA (QUEUE) {0}",
                                _Queue_Read_Pagamento);
        _channel.QueueDeclare(queue: _Queue_Read_Pagamento,
                              durable: PARAMETRI.QUEUE.PROPRIETA.DURABLE,
                              exclusive: PARAMETRI.QUEUE.PROPRIETA.ESCLUSIVE,
                              autoDelete: PARAMETRI.QUEUE.PROPRIETA.AUTODELETE,
                              arguments: null);

    }


    private void AssociazioneQueueESottoscrizioneExchange()
    {
        _logger.LogInformation("ASSOCIAZIONE QUEUE {0} A EXCHANGE {1} e Sottoscrizione evento {2}",
                        _Queue_Read_Pagamento,
                        _Queue_Exchange,
                        _Queue_Read_Pagamento_KeyRouting_Success);

        _channel.QueueBind(queue: _Queue_Read_Pagamento,
                           exchange: _Queue_Exchange,
                           routingKey: _Queue_Read_Pagamento_KeyRouting_Success,
                           arguments: null);

        _logger.LogInformation("ASSOCIAZIONE QUEUE {0} A EXCHANGE {1} e Sottoscrizione evento {2}",
                _Queue_Read_Pagamento,
                _Queue_Exchange,
                _Queue_Read_Pagamento_KeyRouting_Error);

        _channel.QueueBind(queue: _Queue_Read_Pagamento,
                           exchange: _Queue_Exchange,
                           routingKey: _Queue_Read_Pagamento_KeyRouting_Error,
                           arguments: null);

    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
