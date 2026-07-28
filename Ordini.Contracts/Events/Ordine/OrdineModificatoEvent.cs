namespace Ordini.Contracts.Events.Ordine
{

    /// <summary>
    /// ordine modificato
    /// </summary>
    public class OrdineModificatoEvent
    {
        public Guid IdSaga { get; set; } = Guid.NewGuid();

        public string IdOrdine { get; set; } = string.Empty;

        public Ordini.Contracts.Models.Ordine DatoModificato { get; set; }
    }

}
