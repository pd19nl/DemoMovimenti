namespace Ordini.Contracts.Events.Ordine
{
    public class OrdineFinalizzatoEvent
    {
        public Guid IdSaga { get; set; } = Guid.NewGuid();

        public string IdOrdine { get; set; } = string.Empty;
    }
}
