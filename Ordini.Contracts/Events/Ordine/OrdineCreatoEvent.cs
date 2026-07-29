namespace Ordini.Contracts.Events.Ordine
{
    /// <summary>
    /// ordine creato con successo ma incompleto delle fasi successive
    /// i dati sono tutti quelli dell'ordine per non richiamare il recupero delle info ordine
    /// </summary>
    public class OrdineCreatoEvent
    {
        public Guid IdSaga { get; set; } = Guid.NewGuid();

        public string IdOrdine { get; set; } = string.Empty;

        public string IdCliente { get; set; } = string.Empty;

        //public decimal ImportoTotale { get; set; }

        public DateTime Data { get; set; }

        public string Note { get; set; } = string.Empty;

        public List<DettaglioProdottoEvent> Prodotti { get; set; } = new List<DettaglioProdottoEvent>();

        public decimal ImportoTotale
        {
            get =>
            Prodotti.Sum(x => x.ImportoTotale);
        }
    }
}

