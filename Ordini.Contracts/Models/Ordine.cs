namespace Ordini.Contracts.Models
{
    public class Ordine
    {
        public string Id { get; set; } = string.Empty;

        public DateTime Data { get; set; }

        public string IdCliente { get; set; }

        public string? NumProg { get; set; }

        //public string? PuntoVendita { get; set; }

        public string? Note { get; set; }

        public short CodiceStato { get; set; }

        public List<DettaglioOrdine> Dettagli { get; set; } = new List<DettaglioOrdine>();
    }
}
