using Ordini.Contracts.Events.Inventario;
using Ordini.Contracts.Events.Ordine;
using Ordini.Contracts.Events.Pagamento;

namespace Ordini.Contracts
{
    public static class PARAMETRI_GLOBALI
    {
        public static class CONNESSIONE_DB
        {
            public const string MAIN_WR = "MainDbConString";
            public const string MAIN_R = "MainDbConStringReader";
            public const string LOG_ADM = "LogsDbConString";
        }

        public static class QUEUE
        {
            public static class EXCHANGE
            {
                //"movimenti.saga.eventi.exchange.ordini"
                private const string ROOT = "movimenti.saga.eventi.exchange";
                //Dead Letter Exchange (messaggi falliti)
                private const string ROOT_DLE = "movimenti.saga.eventi.exchange.dle";

                private const string ORDINI = "ordini";

                public static string NomeExchangeOrdini { get => ROOT + "." + ORDINI; }
                //Dead Letter Exchange (messaggi falliti)
                public static string NomeExchangeOrdiniDle { get => ROOT_DLE + "." + ORDINI; }
                public static string NomeExchange { get => ROOT; }

            }

            public static class PROPRIETA
            {
                public static string ORDINI_NAME = "ordini.processor.queue";
                //Dead Letter Queue  (messaggi falliti)
                public static string ORDINI_NAME_DLQ = "ordini.processor.queue.dle";

                public static bool AUTODELETE = false;

                public static bool DURABLE = true;

                public static bool ESCLUSIVE = false;

            }

            public static class CHIAVE_EVENTO
            {
                /// <summary>
                /// dato un tipo evento su quale keyrouting key adottare
                /// </summary>
                /// <param name="eventName"></param>
                /// <returns></returns>
                public static string GetRoutingKeyForType(string eventName)
                {
                    string ritorno = "";
                    switch (eventName)
                    {
                        case nameof(OrdineCreatoEvent):
                            ritorno = ORDINE.PROCESSAMENTO_CREATO;
                            break;

                        case nameof(OrdineModificatoEvent):
                            ritorno = ORDINE.PROCESSAMENTO_MODIFICATO;
                            break;

                        case nameof(OrdineCancellatoEvent):
                            ritorno = ORDINE.PROCESSAMENTO_CANCELLATO;
                            break;

                        case nameof(InventarioNonDisponibileEvent):
                            ritorno = INVENTARIO.PROCESSAMENTO_NON_DISPONIBILE;
                            break;

                        case nameof(InventarioRiservatoEvent):
                            ritorno = INVENTARIO.PROCESSAMENTO_ALLOCATA;
                            break;

                        case nameof(PagamentoFallitoEvent):
                            ritorno = PAGAMENTO.PROCESSAMENTO_RESPINTO;
                            break;

                        case nameof(PagamentoRiuscitoEvent):
                            ritorno = PAGAMENTO.PROCESSAMENTO_EFFETTUATO;
                            break;

                        default:
                            ritorno = "eventi.sconosciuti";
                            break;
                    }
                    return ritorno;
                }


                public static class ORDINE
                {
                    //"api.ordine.creazione.richiesta"
                    public const string RICHIESTA_CREAZIONE = "api.ordine.creazione";
                    //"api.ordine.modifica.richiesta"
                    public const string RICHIESTA_MODIFICA = "api.ordine.modifica";
                    public const string RICHIESTA_CANCELLAZIONE = "api.ordine.cancellazione";


                    public const string PROCESSAMENTO_CREATO = "ordine.creato";
                    public const string PROCESSAMENTO_MODIFICATO = "ordine.modificato";
                    public const string PROCESSAMENTO_CANCELLATO = "ordine.cancellato";
                }

                public static class INVENTARIO
                {
                    public const string PROCESSAMENTO_NON_DISPONIBILE = "inventario.nondisponibile";
                    public const string PROCESSAMENTO_ALLOCATA = "inventario.allocata";
                }

                public static class PAGAMENTO
                {
                    public const string PROCESSAMENTO_RESPINTO = "pagamento.respinto";
                    public const string PROCESSAMENTO_EFFETTUATO = "pagamento.riuscito";
                }

            }
        }
    }
}
