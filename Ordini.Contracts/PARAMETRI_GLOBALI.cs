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
                private const string ORDINI = "ordini";

                public static string NomeExchangeOrdini { get => ROOT + "." + ORDINI; }

                public static string NomeExchange { get => ROOT; }

            }

            public static class PROPRIETA
            {
                public static string ORDINI_NAME = "ordini.processor.queue";

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
                            ritorno = ORDINE.CREATO;
                            break;

                        case nameof(OrdineModificatoEvent):
                            ritorno = ORDINE.MODIFICATO;
                            break;

                        case nameof(OrdineCancellatoEvent):
                            ritorno = ORDINE.CANCELLATO;
                            break;

                        case nameof(InventarioNonDisponibileEvent):
                            ritorno = INVENTARIO.NON_DISPONIBILE;
                            break;

                        case nameof(InventarioRiservatoEvent):
                            ritorno = INVENTARIO.ALLOCATA;
                            break;

                        case nameof(PagamentoFallitoEvent):
                            ritorno = PAGAMENTO.RESPINTO;
                            break;

                        case nameof(PagamentoRiuscitoEvent):
                            ritorno = PAGAMENTO.EFFETTUATO;
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
                    public const string RICHIESTA_CREAZIONE = "api.ordine.creazione.richiesta";
                    //"api.ordine.modifica.richiesta"
                    public const string RICHIESTA_MODIFICA = "api.ordine.modifica.richiesta";
                    public const string RICHIESTA_CANCELLAZIONE = "api.ordine.cancellazione.richiesta";


                    public const string CREATO = "ordine.creato";
                    public const string MODIFICATO = "ordine.modificato";
                    public const string CANCELLATO = "ordine.cancellato";
                }

                public static class INVENTARIO
                {
                    public const string NON_DISPONIBILE = "inventario.nondisponibile";
                    public const string ALLOCATA = "inventario.allocata";
                }

                public static class PAGAMENTO
                {
                    public const string RESPINTO = "pagamento.respinto";

                    public const string EFFETTUATO = "pagamento.riuscito";
                }

            }
        }
    }
}
