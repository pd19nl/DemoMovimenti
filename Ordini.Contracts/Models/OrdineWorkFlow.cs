namespace Ordini.Contracts.Models
{
    public class OrdineWorkFlow
    {
        public string Id { get; set; } = string.Empty;

        public DateTime Data { get; set; }

        public eOrdineStato CodiceStato { get; set; } = eOrdineStato.OK_InElaborazione;
    }
}
