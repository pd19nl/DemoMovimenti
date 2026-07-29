using Ordini.Contracts;
using Ordini.Contracts.Events.Ordine;
using Ordini.Contracts.Models.OutBox;

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

        /// attivazione pattern outbox perchè è la parte che crea su tabella e quindi ci deve essere anche
        /// la registrazione sicura dell'evento successivo
        /// 
        public async Task<(string nuovoId, OutBoxMessage outbox)> CreazioneOrderInOutBoxAsync(OrdineRichiestoEvent nuovoOrdine)
        {



        }



    }
}
