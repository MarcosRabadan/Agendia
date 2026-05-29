using FluentValidation;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Waitlist.Commands
{
    public class JoinWaitlistCommandValidator : AbstractValidator<JoinWaitlistCommand>
    {
        public JoinWaitlistCommandValidator(IClock clock)
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.BusinessId).GreaterThan(0);
            RuleFor(x => x.Dto.ServiceId).GreaterThan(0);
            RuleFor(x => x.Dto.EmployeeId).GreaterThan(0).When(x => x.Dto.EmployeeId.HasValue);
            RuleFor(x => x.Dto)
                .Must(d => d.Date.ToDateTime(d.StartTime) > clock.BusinessNow)
                .WithMessage("Solo puedes apuntarte a una franja futura.");
        }
    }
}
