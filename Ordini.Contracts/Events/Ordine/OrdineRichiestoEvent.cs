namespace Ordini.Contracts.Events.Ordine
{

    /// <summary>
    /// richiesta di ordine da parte del client:
    /// avvio saga
    /// tutti i dati ripetuti per non accedere all'ordine
    /// </summary>
    public class OrdineRichiestoEvent
    {
        public Guid IdSaga { get; set; } = Guid.NewGuid();
        public OrdineCreatoEvent Ordine { get; set; } = new OrdineCreatoEvent();


    }
}
