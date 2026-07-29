namespace Ordini.ApplicationAPI.Models.DTOs.Ordine.Modifica
{
    //public enum eTipoModifica
    //{
    //    NessunaModifica = 0,
    //    Aggiunta = 1,
    //    Modifica = 2,
    //    Cancellazione = 3
    //}

    public class EditDettaglioOrdineDTO
    {
        public long Id { get; set; }
        public string CodiceArticolo { get; set; } = string.Empty;
        public int Qta { get; set; }
        public decimal Prezzo { get; set; }

        //public eTipoModifica TipoModifica { get; set; } = eTipoModifica.NessunaModifica;
    }
}
