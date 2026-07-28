using Microsoft.AspNetCore.Diagnostics;

namespace Ordini.Api.Exceptions
{
    public class GloblalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GloblalExceptionHandler> _logger;
        private readonly IHostEnvironment _env;

        public GloblalExceptionHandler(ILogger<GloblalExceptionHandler> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        /// <summary>
        /// geestione eccezioni
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="exception"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var traceId = ActivityTrackingOptions.TraceId.ToString() ?? httpContext.TraceIdentifier;

            //gestione delle risposte da fornire
            //si considerano 4 parametri: statusCode, title, details, errors
            var (statusCode, title, details, errors) = exception switch
            {
                //caso specifico NotFound
                CustomNotFoundException nf => (
                    StatusCodes.Status404NotFound,
                    "Risorsa non trovata",
                    nf.Message,
                    null as object
                ),

                //caso per tutte le altre
                _ => (
                    StatusCodes.Status500InternalServerError,
                    "Errore interno del server.",
                    _env.IsDevelopment() ? exception.Message : "Si è verificato un errore naspettato.",
                    null as object
                )
            };

            //corpo risposta personalizzata
            var problema = new
            {
                status = statusCode,
                title = title,
                detail = details,
                traceId,
                errors
            };

            //log delle informazioni
            _logger.LogError(
                exception,
                "X Errore. StatusCode: {statusCode}, TraceId: {traceId}, Path: {Path}",
                statusCode,
                traceId,
                httpContext.Request.Path);

            //risposta Json al client
            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(problema, cancellationToken);

            return true;
        }
    }
}
