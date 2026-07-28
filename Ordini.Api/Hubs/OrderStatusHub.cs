using Microsoft.AspNetCore.SignalR;
using Ordini.Api.Exceptions;
using Ordini.ApplicationAPI.Models.DTOs.Ordine.Notifiche;

namespace Ordini.Api.Hubs
{
    /// <summary>
    /// gestione comunicazione in real-time
    /// azioni:
    /// - registrazione del browser alle notifiche ordine
    /// - ricevere i messaggi da pubblicare dai worker e distribuirli
    /// </summary>
    public class OrderStatusHub : Hub
    {
        private readonly ILogger<GloblalExceptionHandler> _logger;

        public OrderStatusHub(ILogger<GloblalExceptionHandler> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// registrazione del client
        /// </summary>
        /// <param name="idOrder">ID dell'ordine</param>
        /// <returns></returns>
        public async Task SubscribeToOrder(string idOrder)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, idOrder);
            _logger.LogInformation("Client {ConnectionId} iscritto al gruppo {IdOrder}",
                Context.ConnectionId, idOrder);
        }

        /// <summary>
        /// pubblicazione dei messaggi
        /// </summary>
        /// <returns></returns>
        public async Task SendNotiticationToGroup(string idOrder, eOrdineStatus status, string motivo)
        {

            _logger.LogInformation("Notifica al gruppo {IdOrder}: {status} - {motivo}", idOrder, status, motivo);

            await Clients.Group(idOrder)
                .SendAsync("OrderStatusUpdate", new SignalRMessageDTO() { Status = status, Motivo = motivo });
        }

    }
}
