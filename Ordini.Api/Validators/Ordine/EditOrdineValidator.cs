using FluentValidation;
using Ordini.ApplicationAPI.Models.DTOs.Ordine.Modifica;

namespace Ordini.Api.Validators.Ordine
{
    public class EditOrdineValidator : AbstractValidator<EditOrdineDTO>
    {
        public EditOrdineValidator()
        {
            RuleFor(o => o.Id)
                .NotEmpty().WithMessage("Il Codice Ordine è obbligatorio")
                .MaximumLength(10).WithMessage("Lunghezza Massima Codice Ordine 10");


            RuleFor(o => o.Prodotti)
               .NotEmpty()
                .WithMessage("L'ordine deve contenere almeno un articolo");

            RuleForEach(o => o.Prodotti).SetValidator(new EditDettaglioOrdineValidator());
        }
    }
}
