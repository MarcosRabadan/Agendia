using FluentValidation;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands
{
    /// <summary>
    /// Shared validator for custom time slots used inside CustomHours overrides.
    /// </summary>
    public class CreateCustomTimeSlotDtoValidator : AbstractValidator<CreateCustomTimeSlotDto>
    {
        public CreateCustomTimeSlotDtoValidator()
        {
            RuleFor(x => x.StartTime).NotEqual(default(TimeOnly));
            RuleFor(x => x.EndTime)
                .NotEqual(default(TimeOnly))
                .GreaterThan(x => x.StartTime)
                .WithMessage("EndTime debe ser posterior a StartTime.");
        }
    }
}
