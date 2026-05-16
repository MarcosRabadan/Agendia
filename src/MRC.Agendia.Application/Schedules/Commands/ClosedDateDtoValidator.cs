using FluentValidation;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public class ClosedDateDtoValidator : AbstractValidator<ClosedDateDto>
    {
        public ClosedDateDtoValidator()
        {
            RuleFor(x => x.Date).NotEqual(default(DateOnly));
            RuleFor(x => x.Reason).MaximumLength(500);
        }
    }
}
