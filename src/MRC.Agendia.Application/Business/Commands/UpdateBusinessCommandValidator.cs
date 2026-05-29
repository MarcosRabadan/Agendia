using FluentValidation;

namespace MRC.Agendia.Application.Business.Commands
{
    public class UpdateBusinessCommandValidator : AbstractValidator<UpdateBusinessCommand>
    {
        public UpdateBusinessCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.Id).GreaterThan(0);
            RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Dto.Address).NotEmpty().MaximumLength(500);
            RuleFor(x => x.Dto.Phone).NotEmpty().MaximumLength(50);
            RuleFor(x => x.Dto.Email).NotEmpty().EmailAddress().MaximumLength(200);
            RuleFor(x => x.Dto.Description).MaximumLength(2000);
            RuleFor(x => x.Dto.CancellationWindowHours)
                .InclusiveBetween(1, 8760)
                .When(x => x.Dto.CancellationWindowHours.HasValue)
                .WithMessage("La ventana de cancelacion debe estar entre 1 y 8760 horas; omitela (null) para no aplicar restriccion.");
        }
    }
}
