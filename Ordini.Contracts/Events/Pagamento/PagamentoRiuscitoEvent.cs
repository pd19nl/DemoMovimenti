namespace Ordini.Contracts.Events.Pagamento
{
    /// <summary>
    /// pubblicato da   :   Pagamenti.Processor
    /// Quando          :   pagamento ordine eseguito con successo
    /// Azione Saga     :   success
    /// </summary>
    public class PagamentoRiuscitoEvent
    {
        public string IdOrdine { get; set; } = string.Empty;
    }
}
