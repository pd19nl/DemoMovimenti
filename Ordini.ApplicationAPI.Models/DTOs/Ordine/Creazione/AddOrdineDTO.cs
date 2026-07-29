namespace Ordini.ApplicationAPI.Models.DTOs.Ordine.Creazione
{
    public class AddOrdineDTO
    {

        //public string Id { get; set; } = string.Empty;

        public DateTime Data { get; set; }

        public string IdCliente { get; set; }

        //public string? NumProg { get; set; }

        //public string? PuntoVendita { get; set; }

        public string? Note { get; set; }

        public List<AddDettaglioOrdineDTO> Dettagli { get; set; } = new List<AddDettaglioOrdineDTO>();
    }
}
