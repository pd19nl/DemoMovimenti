
using FluentValidation;

namespace Ordini.Api.Filters
{
    /// <summary>
    /// validazione oggetti di input trasmessi dagli endpoint
    /// richiama i FluentValidation
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ValidationFilter<T> : IEndpointFilter where T : class
    {
        private readonly IValidator<T> _validator;

        public ValidationFilter(IValidator<T> validator)
        {
            _validator = validator;
        }

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            //ricerca dei parametri per il tipo indicato
            var argomentoToValidate = context.Arguments.FirstOrDefault(arg => arg is T);

            //se non trovato salta 
            if (argomentoToValidate is null)
            {
                return await next(context);
            }

            //esecuzione validazione del parametro
            var validazioneRisultato = await _validator.ValidateAsync((T)argomentoToValidate);

            if (!validazioneRisultato.IsValid)
            {
                //validazione fallita
                return Results.ValidationProblem(validazioneRisultato.ToDictionary());
            }

            return await next(context);
        }
    }
}
