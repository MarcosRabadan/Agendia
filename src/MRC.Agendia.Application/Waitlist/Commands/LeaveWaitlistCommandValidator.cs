using FluentValidation;

namespace MRC.Agendia.Application.Waitlist.Commands
{
    public class LeaveWaitlistCommandValidator : AbstractValidator<LeaveWaitlistCommand>
    {
        public LeaveWaitlistCommandValidator()
        {
            RuleFor(x => x.EntryId).GreaterThan(0);
        }
    }
}
