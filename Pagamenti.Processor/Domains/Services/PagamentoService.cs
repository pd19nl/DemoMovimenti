
using Ordini.Contracts.Events.Inventario;
using Ordini.Contracts.Models.Ordini;
using Pagamenti.Processor.Domains.Repositories.Dapper;

namespace Pagamenti.Processor.Domains.Services
{
    public class PagamentoService
    {
        private readonly ILogger<PagamentoService> _logger;
        private readonly PagamentoRepositoryCRUD _repositoryCRUD;

        public PagamentoService(ILogger<PagamentoService> logger, PagamentoRepositoryCRUD repositoryCRUD)
        {
            _logger = logger;
            _repositoryCRUD = repositoryCRUD;
        }


        /// <summary>
        /// cerca di effettuare il pagamento
        /// </summary>
        /// <param name="evento"></param>
        /// <returns></returns>
        public async Task<(bool successo, string? errore)> EffettuaPagamento(InventarioRiservatoEvent evento)
        {
            //if (evento.Ordine.IdCliente.Equals("fallire", StringComparison.OrdinalIgnoreCase))
            if (evento.Ordine.IdCliente == 3)
            {
                _logger.LogWarning("Pagamento Fallito");
                await _repositoryCRUD.SalvaTransazione(evento, eOrdineStato.KO_PagamentoFallito);
                return (false, "Pagamento Fallito");
            }
            _logger.LogWarning("Pagamento Effettuato");
            await _repositoryCRUD.SalvaTransazione(evento, eOrdineStato.OK_PagamentoEseguito);
            return (true, null);
        }

    }
}
