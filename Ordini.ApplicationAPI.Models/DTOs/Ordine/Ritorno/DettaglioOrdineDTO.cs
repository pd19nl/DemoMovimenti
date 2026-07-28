namespace Ordini.ApplicationAPI.Models.DTOs.Ordine.Ritorno
{
    public class DettaglioOrdineDTO
    {
        public long Id { get; set; }

        public string CodiceArticolo { get; set; } = string.Empty;
        public int Qta { get; set; }
        public decimal Prezzo { get; set; }
    }
}
