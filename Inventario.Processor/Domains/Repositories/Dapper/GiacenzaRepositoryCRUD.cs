using Dapper;
using Microsoft.Data.SqlClient;
using Ordini.Contracts;
using Ordini.Contracts.Events.Ordine;
using Ordini.Contracts.Events.Pagamento;
using Ordini.Contracts.Models.Ordini;
using System.Data.Common;

namespace Inventario.Processor.Domains.Repositories.Dapper;

public class GiacenzaRepositoryCRUD
{

    private readonly string _connectionString;
    private readonly ILogger<GiacenzaRepositoryCRUD> _logger;

    public GiacenzaRepositoryCRUD(IConfiguration configuration,
                                ILogger<GiacenzaRepositoryCRUD> logger)
    {
        _connectionString = configuration.GetConnectionString(PARAMETRI.CONNESSIONE_DB.MAIN_R)!;
        _logger = logger;
    }

    /// <summary>
    /// aggiorna le scorte se sono disonibili
    /// </summary>
    /// <param name="evento"></param>
    /// <returns></returns>
    public async Task<(bool successo, string? errore)> ImpegnaScorte(OrdineCreatoEvent evento)
    {
        _logger.LogInformation("Apertura connessione");
        using SqlConnection sqlConnection = new SqlConnection(_connectionString);
        await sqlConnection.OpenAsync();

        _logger.LogInformation("Apertura Transazione");
        using DbTransaction sqlTransaction = await sqlConnection.BeginTransactionAsync();
        try
        {

            DateTime dataOperazione = DateTime.Now;
            foreach (DettaglioProdottoEvent p in evento.Prodotti)
            {
                #region lettura qta disponibile
                string sqlQta = "select QTA_DISPONIBILE from dbo.GIACENZE " +
                    "where COD_ARTICOLO = @art";
                object[] sqlQtaPar =   {
                    new {art = p.CodiceArticolo }
                };
                int qtaDisp = await sqlConnection.QuerySingleOrDefaultAsync<int>(sqlQta, sqlQtaPar, sqlTransaction);
                #endregion


                if (qtaDisp < p.Qta)
                {
                    await sqlTransaction.RollbackAsync();
                    _logger.LogError("QUANTITA NON DISPONIBILI PER ORDINE: {0}", evento.IdOrdine);
                    return (false, $"Prodotto {p.CodiceArticolo} NON DISPONIBILE O ESAURITO.");
                }

                #region aggiornamento qta disponibile
                string sqlQtaUpd = "update dbo.GIACENZE set " +
                                    "QTA_DISPONIBILE = QTA_DISPONIBILE - @qtaord " +
                                    "where COD_ARTICOLO = @art and " +
                                    "QTA_DISPONIBILE = @qta";
                object[] sqlQtaUpdPar =   {
                    new {art = p.CodiceArticolo,
                        qta = qtaDisp,
                        qtaord = p.Qta
                    }
                    };
                int nrRigheAggiornate = await sqlConnection.ExecuteAsync(sqlQtaUpd, sqlQtaUpdPar, sqlTransaction);

                #endregion

                if (nrRigheAggiornate == 1)
                {
                    _logger.LogInformation("SCORTE ALLOCAZIONE PER ORDINE: {0} - Qta: {1}",
                                            evento.IdOrdine,
                                            p.Qta);
                    //NON SI COMMITTA PERCHE' DEVONO ESSERE FATTI TUTTI I PRODOTTI 
                    //CON SUCCESSO
                }
                else
                {
                    await sqlTransaction.RollbackAsync();
                    _logger.LogError("ERRORE DURANTE ALLOCAZIONE RISORSE PER ORDINE: {0}", evento.IdOrdine);
                    return (false, "Quantità modificate prima dell'aggiornamento");
                }

            }

            await RegistrazioneWorkflow(sqlConnection, sqlTransaction,
                                        evento.IdOrdine,
                                        evento.IdSaga.ToString(),
                                        eOrdineStato.OK_ScorteAllocate,
                                        dataOperazione);

            sqlTransaction.Commit();
            _logger.LogInformation("SCORTE ALLOCAZIONE PER ORDINE: {0}",
                                    evento.IdOrdine);

            return (true, "");

        }
        catch (Exception ex)
        {
            await sqlTransaction.RollbackAsync();
            _logger.LogError("ERRORE DURANTE ALLOCAZIONE RISORSE PER ORDINE: {0}", evento.IdOrdine);
            await sqlTransaction.RollbackAsync();
            return (false, ex.Message);
        }
    }


    public async Task<(bool successo, string? errore)> LiberaScorte(PagamentoFallitoEvent evento)
    {
        _logger.LogInformation("Apertura connessione");
        using SqlConnection sqlConnection = new SqlConnection(_connectionString);
        await sqlConnection.OpenAsync();

        _logger.LogInformation("Apertura Transazione");
        using DbTransaction sqlTransaction = await sqlConnection.BeginTransactionAsync();
        try
        {
            foreach (DettaglioProdottoEvent p in evento.Prodotti)
            {

                #region ripristino qta disponibile
                string sqlQtaUpd = "update dbo.GIACENZE set " +
                                    "QTA_DISPONIBILE = QTA_DISPONIBILE + @qtaord " +
                                    "where COD_ARTICOLO = @art ";
                object[] sqlQtaUpdPar =   {
                    new {art = p.CodiceArticolo,
                        qtaord = p.Qta
                    }
                    };
                int nrRigheAggiornate = await sqlConnection.ExecuteAsync(sqlQtaUpd, sqlQtaUpdPar, sqlTransaction);

                #endregion

                if (nrRigheAggiornate == 1)
                {
                    _logger.LogInformation("SCORTE RIALLOCAZIONE PER ORDINE: {0} - Qta: {1}",
                                            evento.IdOrdine,
                                            p.Qta);
                    //NON SI COMMITTA PERCHE' DEVONO ESSERE FATTI TUTTI I PRODOTTI 
                    //CON SUCCESSO
                }
                else
                {
                    //scela di non annullare nulla
                    //await sqlTransaction.RollbackAsync();
                    _logger.LogError("ERRORE DURANTE RIALLOCAZIONE RISORSE PER ORDINE: {0} - NON TROVATO RECORD", evento.IdOrdine);
                    //return (false, "Quantità modificate prima dell'aggiornamento");
                }

            }

            sqlTransaction.Commit();
            _logger.LogInformation("SCORTE RIALLOCAZIONE PER ORDINE: {0}",
                                    evento.IdOrdine);
            return (true, "");

        }
        catch (Exception ex)
        {
            await sqlTransaction.RollbackAsync();
            _logger.LogError("ERRORE DURANTE RIALLOCAZIONE RISORSE PER ORDINE: {0}", evento.IdOrdine);
            await sqlTransaction.RollbackAsync();
            return (false, ex.Message);
        }
    }


    private async Task RegistrazioneWorkflow(SqlConnection sqlConnection,
                                            DbTransaction sqlTransaction,
                                            string idOrdine,
                                            string idSaga,
                                            eOrdineStato stato,
                                            DateTime dataOperazione)
    {

        //Registrazione Workflow ordine
        #region registrazione workflow ordine
        string workfloword = "insert into dbo.ORDINI_WORKFLOW " +
                            "(ID_ORDINE, DATA_OPERAZIONE, ID_STATO, ID_SAGA) " +
                            "values (@id, @data, @stato, @idsaga)";

        _logger.LogInformation("Registrazione workflow ordine: {0}", workfloword);

        object[] wfordparameters = { new {   id = idOrdine,
                                                 data = dataOperazione,
                                                 stato = (short)stato,
                                                 idsaga = idSaga
                                         } };

        await sqlConnection.ExecuteAsync(workfloword, wfordparameters, sqlTransaction);
        #endregion
    }



}
