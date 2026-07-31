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
                        case "OrdineCreatoEvent":
                            ritorno = ORDINE.CREAZIONE;
                            break;

                        case "OrdineModificatoEvent":
                            ritorno = ORDINE.MODIFICA;
                            break;

                        case "OrdineCancellazioneRichiestaEvent":
                            ritorno = ORDINE.CANCELLAZIONE;
                            break;

                        case "InventarioNonDisponibileEvent":
                            ritorno = INVENTARIO.NON_DISPONIBILE;
                            break;

                        case "InventarioRiservatoEvent":
                            ritorno = INVENTARIO.ALLOCATA;
                            break;

                        case "PagamentoFallitoEvent":
                            ritorno = PAGAMENTO.RESPINTO;
                            break;

                        case "PagamentoRiuscitoEvent":
                            ritorno = PAGAMENTO.EFFETTUATO;
                            break;
                    }
                    return ritorno;
                }


                public static class ORDINE
                {
                    //"api.ordine.creazione.richiesta"
                    public const string CREAZIONE = "api.ordine.creazione.richiesta";
                    //"api.ordine.modifica.richiesta"
                    public const string MODIFICA = "api.ordine.modifica.richiesta";
                    public const string CANCELLAZIONE = "api.ordine.cancellazione.richiesta";
                }

                public static class INVENTARIO
                {
                    public const string NON_DISPONIBILE = "api.inventario.creazione.nondisponibile";
                    public const string ALLOCATA = "api.inventario.creazione.allocata";
                }

                public static class PAGAMENTO
                {
                    public const string RESPINTO = "api.pagamento.respinto";

                    public const string EFFETTUATO = "api.pagamento.riuscito";
                }

            }
        }
    }
}
