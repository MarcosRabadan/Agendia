using FluentValidation;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Business.Commands.Update
{
    public class UpdateBusinessCommandValidator : AbstractValidator<UpdateBusinessCommand>
    {
        public UpdateBusinessCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.Id).NotEmpty();
            RuleFor(x => x.Dto.CancellationWindowHours)
                .InclusiveBetween(1, 8760)
                .When(x => x.Dto.CancellationWindowHours.HasValue)
                .WithMessage("La ventana de cancelacion debe estar entre 1 y 8760 horas; omitela (null) para no aplicar restriccion.");
            RuleFor(x => x.Dto.DefaultLanguage)
                .Must(lang => SupportedLanguages.IsSupported(lang))
                .WithMessage("El idioma no esta soportado. Valores validos: es, en, fr.");
            RuleFor(x => x.Dto.DefaultAppointmentStatus)
                .Must(s => s.IsValidInitialStatus())
                .WithMessage("El estado inicial por defecto solo puede ser Pending o Confirmed.");
        }
    }
}
