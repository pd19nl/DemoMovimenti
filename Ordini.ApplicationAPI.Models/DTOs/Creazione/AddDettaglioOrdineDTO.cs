namespace Ordini.ApplicationAPI.Models.DTOs.Creazione
{
    public class AddDettaglioOrdineDTO
    {
        public string CodiceArticolo { get; set; } = string.Empty;
        public int Qta { get; set; }
        public decimal Prezzo { get; set; }
    }
}
