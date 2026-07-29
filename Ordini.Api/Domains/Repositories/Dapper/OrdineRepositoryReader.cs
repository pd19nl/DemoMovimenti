using Dapper;
using Microsoft.Data.SqlClient;
using Ordini.Contracts;
using Ordini.Contracts.Models.Ordini;

namespace Ordini.Api.Domains.Repositories.Dapper
{
    public class OrdineRepositoryReader
    {
        private readonly string _connectionString;

        private readonly ILogger<OrdineRepositoryReader> _logger;

        public OrdineRepositoryReader(IConfiguration configuration, ILogger<OrdineRepositoryReader> logger)
        {
            _connectionString = configuration.GetConnectionString(PARAMETRI_GLOBALI.CONNESSIONE_DB.MAIN_R)!;
            _logger = logger;
        }


        /// <summary>
        /// recupero ordine by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Ordine?> GetOrdineByIdAsync(string id)
        {
            _logger.LogInformation("GetOrdineByIdAsync - IdOrdine:{0}", id);
            if (id.Trim() == string.Empty)
            {
                _logger.LogError("GetOrdineByIdAsync - Parametro non passato");
                throw new InvalidDataException("Parametro non passato");
            }


            if (_connectionString.Trim() == string.Empty)
            {
                _logger.LogError("GetOrdineByIdAsync - Connessione non presente");
                throw new InvalidOperationException("Connessione non presente");
            }

            _logger.LogInformation("GetOrdineByIdAsync - Apertura connessione DB Dapper");

            using var sqlconnection = new SqlConnection(_connectionString);
            await sqlconnection.OpenAsync();

            string sqlOrdine = @"select * from Ordini where id = @id";
            _logger.LogInformation("GetOrdineByIdAsync - Query Get singolo ordine: {0}", sqlOrdine);
            Ordine o = await sqlconnection.QuerySingleOrDefaultAsync<Ordine>(sqlOrdine, new { id });

            if (o != null)
            {
                string sqlDettaglioOrdine = @"select * from DettagliOrdine where IdOrdine = @id";
                _logger.LogInformation("GetOrdineByIdAsync - Query Get dettaglio ordine: {0}", sqlDettaglioOrdine);
                IEnumerable<DettaglioOrdine> elencoDettagli = await sqlconnection.QueryAsync<DettaglioOrdine>(sqlDettaglioOrdine, new { id });

                o.Prodotti.AddRange(elencoDettagli);
                //o.Dettagli= elencoDettagli.ToList();

                return o;
            }
            _logger.LogError("GetOrdineByIdAsync - Ordine non trovato nel database");
            throw new DirectoryNotFoundException($"L'ordine con ID {id} non è stato trovato.");

        }
    }
}
