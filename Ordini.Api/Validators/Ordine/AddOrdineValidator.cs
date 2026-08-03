using FluentValidation;
using Ordini.ApplicationAPI.Models.DTOs.Ordine.Creazione;

namespace Ordini.Api.Validators.Ordine
{
    public class AddOrdineValidator : AbstractValidator<AddOrdineDTO>
    {
        public AddOrdineValidator()
        {
            //RuleFor(o => o.IdCliente)
            //    .NotEmpty().WithMessage("Il Codice Cliente è obbligatorio")
            //    .MaximumLength(10).WithMessage("Lunghezza Massima Codice Cliente 10");
            RuleFor(o => o.IdCliente)
                .GreaterThan(0)
                .WithMessage("Indicare un codice cliente valido");

            RuleFor(o => o.Data)
                .LessThanOrEqualTo(DateTime.Now).WithMessage("La data non può essere futura");

            //RuleFor(o => o.Id)
            //   .NotEmpty()
            //    .WithMessage("Il Codice Ordine è obbligatorio")
            //    .MaximumLength(10).WithMessage("Lunghezza Massima Codice Ordine 10");


            RuleFor(o => o.Prodotti)
               .NotEmpty()
                .WithMessage("L'ordine deve contenere almeno un articolo");

            RuleForEach(o => o.Prodotti).SetValidator(new AddDettaglioOrdineValidator());
        }
    }
}
