using Ordini.ApplicationAPI.Models.DTOs.Ordine.Lettura;
using Ordini.Contracts.Models.Ordini;

namespace Ordini.Api.Helpers.Mapper
{
    public static class OrdineModelToDTO
    {
        public static OrdineDTO MapOrdine(Ordine ordine)
        {
            OrdineDTO r = new OrdineDTO();
            r.Note = ordine.Note;
            r.IdCliente = ordine.IdCliente;
            r.Data = ordine.Data;
            r.Id = ordine.Id;

            foreach (DettaglioOrdine d in ordine.Prodotti)
            {
                r.Dettagli.Add(MapDettaglioOrdine(d));
            }
            return r;
        }

        private static DettaglioOrdineDTO MapDettaglioOrdine(DettaglioOrdine dettaglio)
        {
            DettaglioOrdineDTO dr = new DettaglioOrdineDTO();
            dr.Id = dettaglio.Id;
            dr.Qta = dettaglio.Qta;
            dr.CodiceArticolo = dettaglio.CodiceArticolo;
            dr.Prezzo = dettaglio.Prezzo;

            return dr;
        }
    }
}
