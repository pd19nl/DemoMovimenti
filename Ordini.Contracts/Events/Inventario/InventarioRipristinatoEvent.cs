namespace Ordini.Contracts.Events.Inventario;

/// <summary>
/// pubblicato da   :   Inventario.Processor
/// Quando          :   Rollback ordine: Scorte di un ordine sono stare riassegnate e tolte dall'ordine 
/// Azione Saga     :   Rollback
/// </summary>
public class InventarioRipristinatoEvent
{
    public string IdSaga { get; set; } = string.Empty;
    public string IdOrdine { get; set; } = string.Empty;
}
