using FluentValidation;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands
{
    /// <summary>
    /// Shared validator for weekly time slot inputs. Used by template
    /// create/update commands and by GenerateSchedule's nested templates.
    /// </summary>
    public class CreateWeeklyTimeSlotDtoValidator : AbstractValidator<CreateWeeklyTimeSlotDto>
    {
        public CreateWeeklyTimeSlotDtoValidator()
        {
            RuleFor(x => x.DayOfWeek).IsInEnum();
            RuleFor(x => x.SlotType).IsInEnum();
            RuleFor(x => x.StartTime).NotEqual(default(TimeOnly));
            RuleFor(x => x.EndTime)
                .NotEqual(default(TimeOnly))
                .GreaterThan(x => x.StartTime)
                .WithMessage("EndTime debe ser posterior a StartTime.");
        }
    }
}
