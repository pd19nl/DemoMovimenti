namespace Ordini.Contracts.Events.Ordine;


/// <summary>
/// ordine modificato
/// </summary>
public class OrdineModificatoEvent
{
    public Guid IdSaga { get; set; }

    public string IdOrdine { get; set; } = string.Empty;

    public DateTime Data { get; set; } = DateTime.Now;
    //il contenuto del dato modificato
    public Models.Ordini.Ordine Dato { get; set; }
}
