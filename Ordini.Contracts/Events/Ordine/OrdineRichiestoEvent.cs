namespace Ordini.Contracts.Events.Ordine
{

    /// <summary>
    /// richiesta di ordine da parte del glient:
    /// avvio saga
    /// </summary>
    public class OrdineRichiestoEvent
    {
        public Guid IdSaga { get; set; } = Guid.NewGuid();
        public OrdineCreatoEvent Ordine { get; set; } = new OrdineCreatoEvent();


    }
}
