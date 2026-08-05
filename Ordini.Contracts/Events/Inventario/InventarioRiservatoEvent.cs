using Ordini.Contracts.Events.Ordine;

namespace Ordini.Contracts.Events.Inventario;

/// <summary>
/// pubblicato da   :   Inventario.Processor
/// Quando          :   Scorte di un ordine sono verificate riservate con succeso
/// Azione Saga     :   success
/// </summary>
public class InventarioRiservatoEvent
{
    //riporto le informazioni aggiuntive per poter essere usate dagli eventi success successivi 
    //o di rollbacl
    public OrdineCreatoEvent Ordine { get; set; }
}
