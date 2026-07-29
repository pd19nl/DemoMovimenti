using Ordini.Contracts.Events.Ordine;
using Ordini.Contracts.Models;

namespace Ordini.Api.Helpers.Mapper
{
    public static class OrdineModelToEvent
    {
        public static OrdineCreatoEvent MapOrdineCreato(Ordine ordine)
        {
            OrdineCreatoEvent r = new OrdineCreatoEvent();
            r.Note = ordine.Note;
            r.IdCliente = ordine.IdCliente;
            r.Data = ordine.Data;
            r.IdOrdine = ordine.Id;

            foreach (DettaglioOrdine d in ordine.Prodotti)
            {
                r.Prodotti.Add(MapDettaglioOrdine(d));
            }
            return r;
        }

        private static DettaglioProdottoEvent MapDettaglioOrdine(DettaglioOrdine dettaglio)
        {
            DettaglioProdottoEvent dr = new DettaglioProdottoEvent();
            //dr.Id = dettaglio.Id;
            dr.Qta = dettaglio.Qta;
            dr.CodiceArticolo = dettaglio.CodiceArticolo;
            dr.Prezzo = dettaglio.Prezzo;

            return dr;
        }
    }
}
