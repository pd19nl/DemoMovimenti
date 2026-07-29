namespace Ordini.Contracts.Models.OutBox
{
    //messaggio salvato nella tabella OutBox
    public class OutBoxMessage
    {
        public Guid Id { get; set; }
        public DateTime DataCreazione { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
    }
}
