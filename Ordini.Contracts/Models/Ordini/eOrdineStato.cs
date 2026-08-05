namespace Ordini.Contracts.Models.Ordini;

public enum eOrdineStato
{
    OK_InElaborazione = 0, //avvio richiesta
    OK_ScorteAllocate = 1,  //sono state allocate le scorte
    OK_PagamentoEseguito = 2, //pagamento eseguito con successo
    OK_OrdineConcluso = 3,  //ordine concluso con successo
    KO_ScorteNonPresenti = 4,   //ordine annullato per mancanze scorte
    KO_PagamentoFallito = 5, //ordine annullato per pagamento non riuscito
    KO_ScorteLiberate = 6, //scorte liberate
    KO_OrdineAnnullato = 7, //ordine annullato per un problema interno
    KO_OrdineCancellato = 8 //ordine annullato per richiesta utente
}
