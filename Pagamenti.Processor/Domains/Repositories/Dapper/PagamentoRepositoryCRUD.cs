using Dapper;
using Microsoft.Data.SqlClient;
using Ordini.Contracts;
using Ordini.Contracts.Events.Inventario;
using Ordini.Contracts.Models.Ordini;
using System.Data.Common;

namespace Pagamenti.Processor.Domains.Repositories.Dapper;

public class PagamentoRepositoryCRUD
{

    private readonly string _connectionString;
    private readonly ILogger<PagamentoRepositoryCRUD> _logger;

    public PagamentoRepositoryCRUD(IConfiguration configuration,
                                ILogger<PagamentoRepositoryCRUD> logger)
    {
        _connectionString = configuration.GetConnectionString(PARAMETRI.CONNESSIONE_DB.MAIN_R)!;
        _logger = logger;
    }


    public async Task<(bool esito, string? motivo)> SalvaTransazione(InventarioRiservatoEvent evento, eOrdineStato stato)
    {

        _logger.LogInformation("Apertura connessione");
        using SqlConnection sqlConnection = new SqlConnection(_connectionString);
        await sqlConnection.OpenAsync();

        _logger.LogInformation("Apertura Transazione");
        using DbTransaction sqlTransaction = await sqlConnection.BeginTransactionAsync();

        DateTime OperationDate = DateTime.Now;
        try
        {
            //1) creazione ordine
            #region inserimento pagamento
            string inspag = "insert into dbo.PAGAMENTI " +
                                "(ID_ORDINE, ID_CLIENTE, ID_STATO_PAGAMENTO, DATA_TRANSAZIONE, IMPORTO) " +
                                "values (@idord, @idcliente, @stato, @data, @importo)";

            _logger.LogInformation("Step 1) inserimento pagamento: {0}", inspag);

            object[] inspagparameters = { new {   idord = evento.Ordine.IdOrdine,
                                                    idcliente = evento.Ordine.IdCliente,
                                                    stato = (short)stato,
                                                    data = OperationDate,
                                                  importo=  evento.Ordine.ImportoTotale
                                            } };

            await sqlConnection.ExecuteAsync(inspag, inspagparameters, sqlTransaction);
            #endregion

            await sqlTransaction.CommitAsync();

            return (true, null);

        }
        catch (Exception ex)
        {
            _logger.LogError("ERRORE DURANTE INSERIMENTO PAGAMENTO : {0}", ex.Message);
            await sqlTransaction.RollbackAsync();
        }
        return (true, null);
    }


}
