namespace Ordini.Api.Configurations
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
                public const string ROOT = "movimenti.saga.eventi.exchange.";
                public const string ORDINI = "ordini";

            }
            public static class CHIAVE_EVENTO
            {
                //"api.ordine.creazione.richiesta"
                public const string ORDINE_CREAZIONE = "api.ordine.creazione.richiesta";
                //"api.ordine.modifica.richiesta"
                public const string ORDINE_MODIFICA = "api.ordine.modifica.richiesta";
            }
        }
    }
}
