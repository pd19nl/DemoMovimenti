namespace Ordini.Contracts.Models
{
    public class Articolo
    {
        public string Codice { get; set; } = string.Empty;

        public string Descrizione { get; set; } = string.Empty;

        public bool FlgAttivo { get; set; }

        public DateTime DataAttivazione { get; set; } = DateTime.Now;
    }
}
