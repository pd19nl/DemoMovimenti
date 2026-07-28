namespace Ordini.ApplicationAPI.Models.DTOs.Ordine.Ritorno
{
    public class OrdineDTO
    {

        public string Id { get; set; } = string.Empty;

        public DateTime Data { get; set; }

        public string IdCliente { get; set; }

        //public string? NumProg { get; set; }

        //public string? PuntoVendita { get; set; }

        public string? Note { get; set; }

        public List<DettaglioOrdineDTO> Dettagli { get; set; } = new List<DettaglioOrdineDTO>();
    }
}
