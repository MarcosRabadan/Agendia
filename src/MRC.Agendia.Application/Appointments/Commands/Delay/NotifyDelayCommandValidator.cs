using FluentValidation;

namespace MRC.Agendia.Application.Appointments.Commands.Delay
{
    public class NotifyDelayCommandValidator : AbstractValidator<NotifyDelayCommand>
    {
        private const int MaxDelayMinutes = 600;

        public NotifyDelayCommandValidator()
        {
            RuleFor(x => x.BusinessId).GreaterThan(0);
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.DelayMinutes)
                .InclusiveBetween(1, MaxDelayMinutes)
                .WithMessage($"Los minutos de retraso deben estar entre 1 y {MaxDelayMinutes}.");
            RuleFor(x => x.Dto.EmployeeId).GreaterThan(0).When(x => x.Dto.EmployeeId.HasValue);
            RuleFor(x => x.Dto.MaxAppointments).GreaterThan(0).When(x => x.Dto.MaxAppointments.HasValue);
        }
    }
}
