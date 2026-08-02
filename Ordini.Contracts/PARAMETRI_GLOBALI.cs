using Ordini.Contracts.Events.Inventario;
using Ordini.Contracts.Events.Ordine;
using Ordini.Contracts.Events.Pagamento;

namespace Ordini.Contracts
{
    //PARAMETRI GLOBALI
    public static class PARAMETRI
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

            public static class KEY_EVENTO
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
                            ritorno = ORDINE.PROCESSATO.CREATO;
                            break;

                        case nameof(OrdineModificatoEvent):
                            ritorno = ORDINE.PROCESSATO.MODIFICATO;
                            break;

                        case nameof(OrdineCancellatoEvent):
                            ritorno = ORDINE.PROCESSATO.CANCELLATO;
                            break;

                        case nameof(InventarioNonDisponibileEvent):
                            ritorno = INVENTARIO.PROCESSATO.NON_DISPONIBILE;
                            break;

                        case nameof(InventarioRiservatoEvent):
                            ritorno = INVENTARIO.PROCESSATO.ALLOCATA;
                            break;

                        case nameof(PagamentoFallitoEvent):
                            ritorno = PAGAMENTO.PROCESSATO.RESPINTO;
                            break;

                        case nameof(PagamentoRiuscitoEvent):
                            ritorno = PAGAMENTO.PROCESSATO.EFFETTUATO;
                            break;

                        default:
                            ritorno = "eventi.sconosciuti";
                            break;
                    }
                    return ritorno;
                }


                public static class ORDINE
                {
                    public static class RICHIESTA
                    {
                        //"api.ordine.creazione.richiesta"
                        public const string CREAZIONE = "api.ordine.creazione";
                        //"api.ordine.modifica.richiesta"
                        public const string MODIFICA = "api.ordine.modifica";
                        public const string CANCELLAZIONE = "api.ordine.cancellazione";
                    }

                    public static class PROCESSATO
                    {
                        public const string CREATO = "ordine.creato";
                        public const string MODIFICATO = "ordine.modificato";
                        public const string CANCELLATO = "ordine.cancellato";
                    }
                }

                public static class INVENTARIO
                {
                    public static class PROCESSATO
                    {
                        public const string NON_DISPONIBILE = "inventario.nondisponibile";
                        public const string ALLOCATA = "inventario.allocata";
                    }
                }

                public static class PAGAMENTO
                {
                    public static class PROCESSATO
                    {
                        public const string RESPINTO = "pagamento.respinto";
                        public const string EFFETTUATO = "pagamento.riuscito";
                    }
                }

            }
        }
    }
}
