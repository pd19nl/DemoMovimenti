using Ordini.Contracts;

namespace Ordini.Processor.Domains.Repositories.Dapper
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



    }
}
