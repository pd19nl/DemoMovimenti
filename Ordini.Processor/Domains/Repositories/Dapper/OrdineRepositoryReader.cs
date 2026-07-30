using Dapper;
using Microsoft.Data.SqlClient;
using Ordini.Contracts;
using Ordini.Contracts.Events.Ordine;
using Ordini.Contracts.Models.Ordini;
using Ordini.Contracts.Models.OutBox;
using System.Data.Common;
using System.Text.Json;

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
        public async Task<(string nuovoId, OutBoxMessage outbox)> CreazioneOrderOutBoxAsync(OrdineRichiestoEvent nuovoOrdine)
        {
            _logger.LogInformation("Apertura connessione");
            using var sqlConnection = new SqlConnection(_connectionString);
            await sqlConnection.OpenAsync();

            _logger.LogInformation("Apertura Transazione");
            using DbTransaction sqlTransaction = await sqlConnection.BeginTransactionAsync();

            DateTime OperationDate = DateTime.Now;
            try
            {
                //1) creazione ordine
                #region creazione ordine
                string insord = "insert into dbo.ORDINI " +
                                    "(ID, DATA_CREAZIONE, ID_CLIENTE, NOTE, ID_STATO) " +
                                    "values (@id, @data, @idcliente, @note, @stsato)";

                _logger.LogInformation("Step 1) inserimento ordine: {0}", insord);

                object[] insordparameters = { new {   id = nuovoOrdine.Ordine.IdOrdine,
                                                data = OperationDate,
                                                idcliente = nuovoOrdine.Ordine.IdCliente,
                                                note=  nuovoOrdine.Ordine.Note,
                                                stsato = (int)eOrdineStato.OK_InElaborazione
                                             } };

                await sqlConnection.ExecuteAsync(insord, insordparameters, sqlTransaction);
                #endregion

                #region creazione dettaglio ordine
                //2) inserimento dettagli ordine
                string sqlinsorddet = "";
                foreach (DettaglioProdottoEvent de in nuovoOrdine.Ordine.Prodotti)
                {
                    _logger.LogInformation("Step 2) inserimento dettaglio ordine: {0}", de.CodiceArticolo);
                    sqlinsorddet = "insert into dbo.ORDINE_DETTAGLI " +
                                    "(ID_ORDINE, COD_ARTICOLO, QTA, PREZZO) " +
                                    "values (@id, @art, @qta, @prz);";

                    object[] sqlinsorddetparameters = {
                        new
                        {
                            id = nuovoOrdine.Ordine.IdOrdine,
                            art = de.CodiceArticolo,
                            qta = de.Qta,
                            prz = de.Prezzo
                        } };

                    await sqlConnection.ExecuteAsync(sqlinsorddet, sqlinsorddetparameters, sqlTransaction);
                }
                #endregion

                #region creazione outbox
                //3) popolamento OutBox
                _logger.LogInformation("Step 3) inserimento richiesta in outbox");

                OutBoxMessage messaggioOutBox = new OutBoxMessage()
                {
                    Id = nuovoOrdine.IdSaga,
                    DataCreazione = OperationDate,
                    TipologiaEvento = nameof(OrdineCreatoEvent),
                    Payload = JsonSerializer.Serialize(nuovoOrdine.Ordine)
                };
                string insoutbox = "insert into dbo.OUTBOX " +
                                    "(ID, DATA_CREAZIONE, TIPLOGIA_EVENTO, PAYLOAD) " +
                                    "values (@Id, @DataCreazione, @TipologiaEvento, @Payload);";


                await sqlConnection.ExecuteAsync(insoutbox, messaggioOutBox, sqlTransaction);
                #endregion

                return (nuovoOrdine.Ordine.IdOrdine, messaggioOutBox);

            }
            catch (Exception ex)
            {
                _logger.LogError("eRRORE DURANTE CREAZIONE ORDINE E OUTBOX: {0}", ex.Message);
                await sqlTransaction.RollbackAsync();
                throw ex;
            }

        }



    }
}
