using FluentValidation;
using Ordini.ApplicationAPI.Models.DTOs.Ordine.Modifica;

namespace Ordini.Api.Validators.Ordine
{
    public class EditDettaglioOrdineValidator : AbstractValidator<EditDettaglioOrdineDTO>
    {

        public EditDettaglioOrdineValidator()
        {
            RuleFor(o => o.CodiceArticolo)
                .NotEmpty()
                .WithMessage("Il Codice Articolo è obbligatorio")
                .MaximumLength(10).WithMessage("Lunghezza Massima Codice Articolo 10");

            RuleFor(o => o.Qta)
                .GreaterThan(0)
                .WithMessage("La quantità minima è 1");

            RuleFor(o => o.Prezzo)
                .GreaterThanOrEqualTo(0)
                .WithMessage("il prezzo non può essere negativo");

            RuleFor(o => o.Id)
                .GreaterThan(0)
                .WithMessage("Codice Id non valido");
        }
    }
}
