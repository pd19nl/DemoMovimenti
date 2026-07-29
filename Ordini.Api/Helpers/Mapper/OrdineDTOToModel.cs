using Ordini.ApplicationAPI.Models.DTOs.Ordine.Creazione;
using Ordini.Contracts.Models;

namespace Ordini.Api.Helpers.Mapper
{
    public static class OrdineDTOToModel
    {
        public static Ordine MapOrdine(AddOrdineDTO ordine)
        {
            Ordine r = new Ordine();
            r.Note = ordine.Note;
            r.IdCliente = ordine.IdCliente;
            r.Data = ordine.Data;
            //r.Id = ordine.Id;

            foreach (AddDettaglioOrdineDTO d in ordine.Dettagli)
            {
                r.Prodotti.Add(MapDettaglioOrdine(d));
            }
            return r;
        }

        private static DettaglioOrdine MapDettaglioOrdine(AddDettaglioOrdineDTO dettaglio)
        {
            DettaglioOrdine dr = new DettaglioOrdine();

            dr.Qta = dettaglio.Qta;
            dr.CodiceArticolo = dettaglio.CodiceArticolo;
            dr.Prezzo = dettaglio.Prezzo;

            return dr;
        }
    }
}
