namespace Ordini.Contracts.Events.Ordine;

public class DettaglioProdottoEvent
{
    public string CodiceArticolo { get; set; }
    public int Qta { get; set; }
    public decimal Prezzo { get; set; }

    public decimal ImportoTotale { get => Qta * Prezzo; }
}
