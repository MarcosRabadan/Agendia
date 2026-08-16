using FluentValidation;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Waitlist.Commands.Join
{
    public class JoinWaitlistCommandValidator : AbstractValidator<JoinWaitlistCommand>
    {
        public JoinWaitlistCommandValidator(IClock clock)
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.BusinessId).NotEmpty();
            RuleFor(x => x.Dto.ServiceId).NotEmpty();
            RuleFor(x => x.Dto.EmployeeId).NotEqual(Guid.Empty).When(x => x.Dto.EmployeeId.HasValue);
            RuleFor(x => x.Dto)
                .Must(d => d.Date.ToDateTime(d.StartTime) > clock.BusinessNow)
                .WithMessage("You can only join a future slot.");
        }
    }
}
