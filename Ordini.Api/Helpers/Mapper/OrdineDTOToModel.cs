using Ordini.ApplicationAPI.Models.DTOs.Ordine.Creazione;
using Ordini.ApplicationAPI.Models.DTOs.Ordine.Modifica;
using Ordini.Contracts.Models;

namespace Ordini.Api.Helpers.Mapper
{
    public static class OrdineDTOToModel
    {
        public static Ordine MapOrdine_Add(AddOrdineDTO ordine)
        {
            Ordine r = new Ordine();
            r.Note = ordine.Note;
            r.IdCliente = ordine.IdCliente;
            r.Data = ordine.Data;
            //r.Id = ordine.Id;

            foreach (AddDettaglioOrdineDTO d in ordine.Prodotti)
            {
                r.Prodotti.Add(MapDettaglioOrdine_Add(d));
            }
            return r;
        }

        private static DettaglioOrdine MapDettaglioOrdine_Add(AddDettaglioOrdineDTO dettaglio)
        {
            DettaglioOrdine dr = new DettaglioOrdine();

            dr.Qta = dettaglio.Qta;
            dr.CodiceArticolo = dettaglio.CodiceArticolo;
            dr.Prezzo = dettaglio.Prezzo;

            return dr;
        }



        public static Ordine MapOrdine_Edit(EditOrdineDTO ordine)
        {
            Ordine r = new Ordine();
            r.Note = ordine.Note;
            //r.IdCliente = ordine.IdCliente;
            //r.Data = ordine.Data;
            r.Id = ordine.Id;


            foreach (EditDettaglioOrdineDTO d in ordine.Prodotti)
            {
                r.Prodotti.Add(MapDettaglioOrdine_Edit(d));
            }
            return r;
        }



        private static DettaglioOrdine MapDettaglioOrdine_Edit(EditDettaglioOrdineDTO dettaglio)
        {
            DettaglioOrdine dr = new DettaglioOrdine();

            dr.Qta = dettaglio.Qta;
            dr.CodiceArticolo = dettaglio.CodiceArticolo;
            dr.Prezzo = dettaglio.Prezzo;
            dr.Id = dettaglio.Id;

            return dr;
        }

    }
}
