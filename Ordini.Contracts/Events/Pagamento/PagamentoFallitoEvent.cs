using Ordini.Contracts.Events.Ordine;

namespace Ordini.Contracts.Events.Pagamento
{
    /// <summary>
    /// pubblicato da   :   Pagamenti.Processor
    /// Quando          :   pagamento ordine fallito
    /// Azione Saga     :   RollBack
    /// </summary>
    public class PagamentoFallitoEvent
    {
        public string IdOrdine { get; set; } = string.Empty;

        public string Motivo { get; set; }

        //per facilitare le azioni di rollback
        public List<DettaglioProdotto> Dettagli { get; set; } = new List<DettaglioProdotto>();
    }
}
