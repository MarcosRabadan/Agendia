using FluentValidation;

namespace MRC.Agendia.Application.Services.Commands
{
    public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
    {
        public CreateServiceCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.BusinessId).GreaterThan(0);
            RuleFor(x => x.Dto.Name).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Dto.Description).MaximumLength(2000);
            RuleFor(x => x.Dto.DurationMinutes)
                .GreaterThan(0).WithMessage("DurationMinutes debe ser mayor que 0.")
                .LessThanOrEqualTo(24 * 60).WithMessage("DurationMinutes no puede superar las 24 horas.");
            RuleFor(x => x.Dto.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Price no puede ser negativo.");
        }
    }
}
