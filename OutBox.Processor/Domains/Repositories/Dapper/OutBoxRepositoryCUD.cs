using Dapper;
using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace OutBox.Processor.Domains.Repositories.Dapper;

/// <summary>
/// Repository destinato alla inserimento, aggiornamento, cancellazione
/// </summary>
public class OutBoxRepositoryCUD
{
    private readonly string _connectionString;
    private readonly ILogger<OutBoxRepositoryCUD> _logger;
    private readonly OutBoxRepositoryReader _outboxLettura;

    public OutBoxRepositoryCUD(string connectionString,
                            ILogger<OutBoxRepositoryCUD> logger,
                            OutBoxRepositoryReader outboxLettura)
    {
        _connectionString = connectionString;
        _logger = logger;
        _outboxLettura = outboxLettura;
    }



    public async Task<bool> UpdateProcessatoSuccess(string idOutbox)
    {
        _logger.LogInformation("Aggiornamento record OutBox [{0}] con esito Success",
                                idOutbox);

        _logger.LogInformation("Apertura connessione");
        using var sqlConnection = new SqlConnection(_connectionString);
        await sqlConnection.OpenAsync();


        _logger.LogInformation("Apertura Transazione");
        using DbTransaction sqlTransaction = await sqlConnection.BeginTransactionAsync();

        DateTime OperationDate = DateTime.Now;

        #region aggiornamento ordine
        string updOutBox = "update dbo.OUTBOX set " +
                        "FLG_PROCESSATO = 1, " +
                        "DATA_ELABORAZIONE = GETDATE() " +
                        "where ID =@id";

        _logger.LogInformation("Aggiornamento record OutBox: {0}", updOutBox);

        object[] updOutBoxParameters = { new { id = idOutbox } };

        var nrRigheAggiornate = await sqlConnection.ExecuteAsync(updOutBox, updOutBoxParameters, sqlTransaction);
        #endregion

        return nrRigheAggiornate == 1;

    }



    public async Task<bool> UpdateProcessatoError(string idOutbox,
                                                string errorMessage)
    {
        _logger.LogInformation("Aggiornamento record OutBox [{0}] con esito fallito",
                                idOutbox);

        _logger.LogInformation("Apertura connessione");
        using var sqlConnection = new SqlConnection(_connectionString);
        await sqlConnection.OpenAsync();


        _logger.LogInformation("Apertura Transazione");
        using DbTransaction sqlTransaction = await sqlConnection.BeginTransactionAsync();

        DateTime OperationDate = DateTime.Now;

        #region aggiornamento ordine
        string updOutBox = "update dbo.OUTBOX set " +
                        "FLG_BLACK_LIST = 1 " +
                        "NOTE_ERRORE = @errore, " +
                        "DATA_ELABORAZIONE = GETDATE() " +
                        "where ID = @id";

        _logger.LogInformation("Aggiornamento record OutBox: {0}", updOutBox);

        object[] updOutBoxParameters = { new { id = idOutbox,
                                                errore = errorMessage} };

        var nrRigheAggiornate = await sqlConnection.ExecuteAsync(updOutBox, updOutBoxParameters, sqlTransaction);
        #endregion

        return nrRigheAggiornate == 1;

    }

}
