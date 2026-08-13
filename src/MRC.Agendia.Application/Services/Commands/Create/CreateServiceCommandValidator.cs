using FluentValidation;

namespace MRC.Agendia.Application.Services.Commands.Create
{
    public class CreateServiceCommandValidator : AbstractValidator<CreateServiceCommand>
    {
        public CreateServiceCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.BusinessId).GreaterThan(0);
            RuleFor(x => x.Dto.DurationMinutes)
                .GreaterThan(0).WithMessage("DurationMinutes debe ser mayor que 0.")
                .LessThanOrEqualTo(24 * 60).WithMessage("DurationMinutes no puede superar las 24 horas.");
        }
    }
}
