namespace Ordini.Contracts.Models
{
    public class Ordine
    {
        public string Id { get; set; } = string.Empty;

        public DateTime Data { get; set; }

        public string IdCliente { get; set; }

        //public string? NumProg { get; set; }

        //public string? PuntoVendita { get; set; }

        public string? Note { get; set; }

        public eOrdineStato CodiceStato { get; set; } = eOrdineStato.OK_InElaborazione;

        public List<DettaglioOrdine> Prodotti { get; set; } = new List<DettaglioOrdine>();

        public decimal ImportoTotale
        {
            get =>
            Prodotti.Sum(x => x.ImportoTotale);
        }
    }
}
