namespace Ordini.Contracts.Events.Inventario
{
    /// <summary>
    /// pubblicato da   :   Inventario.Processor
    /// Quando          :   Success: Scorte di un ordine non disponibili almeno per un prodotto
    /// Azione Saga     :   Rollback
    /// </summary>
    public class InventarioNonDisponibileEvent
    {
        public string IdOrdine { get; set; } = string.Empty;

        public string IdSaga { get; set; } = string.Empty;
        public string Motivo { get; set; }
    }
}
