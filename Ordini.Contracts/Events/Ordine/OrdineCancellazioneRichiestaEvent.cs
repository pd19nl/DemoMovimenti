namespace Ordini.Contracts.Events.Ordine;

/// <summary>
/// richiesta del client di cancellazione ordine
/// </summary>
public class OrdineCancellazioneRichiestaEvent
{
    public Guid IdSaga { get; set; } = Guid.NewGuid();

    public string IdOrdine { get; set; } = string.Empty;

}
