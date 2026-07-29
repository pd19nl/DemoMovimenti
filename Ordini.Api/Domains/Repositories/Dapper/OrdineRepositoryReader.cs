using Dapper;
using Microsoft.Data.SqlClient;
using Ordini.Api.Configurations;
using Ordini.Contracts.Models;

namespace Ordini.Api.Domains.Repositories.Dapper
{
    public class OrdineRepositoryReader
    {
        private readonly string _connectionString;

        public OrdineRepositoryReader(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString(PARAMETRI_GLOBALI.CONNESSIONE_DB.MAIN_R)!;
        }


        /// <summary>
        /// recupero ordine by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Ordine?> GetOrdineByIdAsync(string id)
        {

            if (id.Trim() == string.Empty) throw new InvalidDataException("Parametro non passato");

            if (_connectionString.Trim() == string.Empty) throw new InvalidOperationException("Connessione non presente");


            using var sqlconnection = new SqlConnection(_connectionString);
            await sqlconnection.OpenAsync();

            string sqlOrdine = @"select * from Ordini where id = @id";
            Ordine o = await sqlconnection.QuerySingleOrDefaultAsync<Ordine>(sqlOrdine, new { id });

            if (o != null)
            {
                string sqlDettaglioOrdine = @"select * from DettagliOrdine where IdOrdine = @id";
                IEnumerable<DettaglioOrdine> elencoDettagli = await sqlconnection.QueryAsync<DettaglioOrdine>(sqlDettaglioOrdine, new { id });

                o.Prodotti.AddRange(elencoDettagli);
                //o.Dettagli= elencoDettagli.ToList();

                return o;
            }

            throw new DirectoryNotFoundException($"L'ordine con ID {id} non è stato trovato.");

        }
    }
}
