using Ordini.ApplicationAPI.Models.DTOs.Ordine.Ritorno;
using Ordini.Contracts.Models;

namespace Ordini.Api.Helpers.Mapper
{
    public class OrdineModelToDTO
    {
        public static OrdineDTO MapOrdineToDTO(Ordine ordine)
        {
            OrdineDTO r = new OrdineDTO();
            r.Note = ordine.Note;
            r.IdCliente = ordine.IdCliente;
            r.Data = ordine.Data;
            r.Id = ordine.Id;

            foreach (DettaglioOrdine d in ordine.Dettagli)
            {
                r.Dettagli.Add(MapDettaglioOrdineToDTO(d));
            }
            return r;
        }

        private static DettaglioOrdineDTO MapDettaglioOrdineToDTO(DettaglioOrdine dettaglio)
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
