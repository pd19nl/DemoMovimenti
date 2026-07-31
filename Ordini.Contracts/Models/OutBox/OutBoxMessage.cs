namespace Ordini.Contracts.Models.OutBox
{
    //messaggio salvato nella tabella OutBox
    public class OutBoxMessage
    {
        public Guid Id { get; set; }
        public DateTime DataCreazione { get; set; }
        public string TipologiaEvento { get; set; } = string.Empty; //nome della classe evento
        public string Payload { get; set; } = string.Empty;

        public bool FlgProcessato { get; set; } = false;

        public bool FlgBlackList { get; set; } = false;

        public DateTime? DataElaborazione { get; set; }


        public string? NoteErrore { get; set; }
    }
}
