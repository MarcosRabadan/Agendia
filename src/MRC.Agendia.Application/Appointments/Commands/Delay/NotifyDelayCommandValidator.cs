using FluentValidation;

namespace MRC.Agendia.Application.Appointments.Commands.Delay
{
    public class NotifyDelayCommandValidator : AbstractValidator<NotifyDelayCommand>
    {
        private const int MaxDelayMinutes = 600;

        public NotifyDelayCommandValidator()
        {
            RuleFor(x => x.BusinessId).NotEmpty();
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.DelayMinutes)
                .InclusiveBetween(1, MaxDelayMinutes)
                .WithMessage($"The delay minutes must be between 1 and {MaxDelayMinutes}.");
            RuleFor(x => x.Dto.EmployeeId).NotEqual(Guid.Empty).When(x => x.Dto.EmployeeId.HasValue);
            RuleFor(x => x.Dto.MaxAppointments).GreaterThan(0).When(x => x.Dto.MaxAppointments.HasValue);
        }
    }
}
