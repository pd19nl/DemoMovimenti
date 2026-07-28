namespace Ordini.ApplicationAPI.Models.DTOs.Modifica
{
    public class DeleteOrdineDTO
    {

        public string Id { get; set; } = string.Empty;

        public string? Note { get; set; }

        List<EditDettaglioOrdineDTO> Dettagli { get; set; } = new List<EditDettaglioOrdineDTO>();
    }
}
