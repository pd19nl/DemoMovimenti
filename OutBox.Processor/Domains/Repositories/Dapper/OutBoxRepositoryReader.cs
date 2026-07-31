using Dapper;
using Microsoft.Data.SqlClient;
using Ordini.Contracts.Models.OutBox;

namespace OutBox.Processor.Domains.Repositories.Dapper;

/// <summary>
/// Repository destinato alla lettura
/// </summary>
public class OutBoxRepositoryReader
{
    private readonly string _connectionString;
    private readonly ILogger<OutBoxRepositoryReader> _logger;

    private readonly short _nr_di_record_da_leggere_alla_volta = 20;

    public OutBoxRepositoryReader(string connectionString,
                                    ILogger<OutBoxRepositoryReader> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<List<OutBoxMessage>> LetturaMessaggiDaElaborare()
    {
        if (_connectionString.Trim() == string.Empty)
        {
            _logger.LogError("LetturaMessaggiDaElaborare - Connessione non presente");
            throw new InvalidOperationException("Connessione non presente");
        }

        _logger.LogInformation("LetturaMessaggiDaElaborare - Apertura connessione DB Dapper");

        using var sqlconnection = new SqlConnection(_connectionString);
        await sqlconnection.OpenAsync();

        string sqlOutBox = @"select top(" + _nr_di_record_da_leggere_alla_volta + ") * from dbo.OUTBOX " +
                            "where FLG_PROCESSATO= 0 and FLG_BLACK_LIST = 0 " +
                            "order by DATA_CREAZIONE asc";
        _logger.LogInformation("LetturaMessaggiDaElaborare - Query Get singolo ordine: {0}", sqlOutBox);
        IEnumerable<OutBoxMessage> r = await sqlconnection.QueryAsync<OutBoxMessage>(sqlOutBox);

        if (r != null && r.Count() > 0)
        {
            _logger.LogError("LetturaMessaggiDaElaborare - Trovati Nr Record: {0}", r.Count());
            return r.ToList();
        }
        _logger.LogError("LetturaMessaggiDaElaborare - Nessun record trovato");
        return null;
    }
}
