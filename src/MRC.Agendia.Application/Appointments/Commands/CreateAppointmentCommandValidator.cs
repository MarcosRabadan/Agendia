using FluentValidation;

namespace MRC.Agendia.Application.Appointments.Commands
{
    public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
    {
        public CreateAppointmentCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.ClientId).GreaterThan(0);
            RuleFor(x => x.Dto.EmployeeId).GreaterThan(0);
            RuleFor(x => x.Dto.ServiceId).GreaterThan(0);
            RuleFor(x => x.Dto.StartDate)
                .NotEqual(default(DateTime)).WithMessage("StartDate es obligatorio.");
            RuleFor(x => x.Dto.EndDate)
                .NotEqual(default(DateTime)).WithMessage("EndDate es obligatorio.")
                .GreaterThan(x => x.Dto.StartDate)
                .WithMessage("EndDate debe ser posterior a StartDate.");
            RuleFor(x => x.Dto.Notes).MaximumLength(2000);
            When(x => x.Dto.ExtraServiceIds is { Count: > 0 }, () =>
            {
                RuleForEach(x => x.Dto.ExtraServiceIds).GreaterThan(0);
                RuleFor(x => x.Dto.ExtraServiceIds!.Count)
                    .LessThanOrEqualTo(10)
                    .WithMessage("No se pueden combinar mas de 10 servicios adicionales.");
                RuleFor(x => x.Dto)
                    .Must(d => d.ExtraServiceIds!.Distinct().Count() == d.ExtraServiceIds!.Count
                               && !d.ExtraServiceIds!.Contains(d.ServiceId))
                    .WithMessage("Los servicios adicionales no pueden repetirse ni coincidir con el servicio principal.");
            });
        }
    }
}
