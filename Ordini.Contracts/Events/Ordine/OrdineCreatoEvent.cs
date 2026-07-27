namespace Ordini.Contracts.Events.Ordine
{
    /// <summary>
    /// ordine creato con successo ma incompleto delle fasi
    /// </summary>
    public class OrdineCreatoEvent
    {
        public string IdOrdine { get; set; } = string.Empty;

        public string CodiceCliente { get; set; } = string.Empty;

        public decimal ImportoTotale { get; set; }

        public List<DettaglioProdotto> DettaglioProdotti { get; set; } = new List<DettaglioProdotto>();
    }

    public record DettaglioProdotto(string codArt, int qta, decimal prezzo);
}
