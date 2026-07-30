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
