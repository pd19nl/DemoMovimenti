namespace Ordini.Contracts.Models.Ordini
{
    public class DettaglioOrdine
    {
        public long Id { get; set; }
        //public string IdOrdine { get; set; } = string.Empty;

        public string CodiceArticolo { get; set; } = string.Empty;
        public int Qta { get; set; }
        public decimal Prezzo { get; set; }


        public decimal ImportoTotale { get => Qta * Prezzo; }
    }
}
