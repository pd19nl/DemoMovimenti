using Inventario.Processor.Domains.Repositories.Dapper;
using Ordini.Contracts;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

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
        _logger.LogInformation("SOTTOSCRIZIONE AD EVENTO {0}", PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.ORDINE.PROCESSAMENTO_CREATO);
        _channel.QueueBind(queue: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.ORDINI_NAME,
                           exchange: PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                           routingKey: PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.ORDINE.PROCESSAMENTO_CREATO);

        //  caso fallimento dalla saga da parte del pagamento
        _logger.LogInformation("SOTTOSCRIZIONE AD EVENTO {0}", PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.PAGAMENTO.PROCESSAMENTO_RESPINTO);
        _channel.QueueBind(queue: PARAMETRI_GLOBALI.QUEUE.PROPRIETA.ORDINI_NAME,
                           exchange: PARAMETRI_GLOBALI.QUEUE.EXCHANGE.NomeExchangeOrdini,
                           routingKey: PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.PAGAMENTO.PROCESSAMENTO_RESPINTO);

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
            var ordineServiceDB = scope.ServiceProvider.GetRequiredService<GiacenzaRepositoryCRUD>();

            switch (routingKey)
            {
                case PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.ORDINE.PROCESSAMENTO_CREATO:
                    await Gestione_Giacenza_Richiesta(messaggio, ordineServiceDB);
                    break;

                case PARAMETRI_GLOBALI.QUEUE.CHIAVE_EVENTO.PAGAMENTO.PROCESSAMENTO_RESPINTO:
                    await Gestione_Giacenza_Ripristina(messaggio, ordineServiceDB);
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
