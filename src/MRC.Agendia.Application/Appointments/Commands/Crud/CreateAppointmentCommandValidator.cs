using FluentValidation;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Appointments.Commands.Crud
{
    public class CreateAppointmentCommandValidator : AbstractValidator<CreateAppointmentCommand>
    {
        public CreateAppointmentCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.ClientUserId).NotEmpty();
            RuleFor(x => x.Dto.EmployeeId).NotEmpty();
            RuleFor(x => x.Dto.ServiceId).NotEmpty();
            RuleFor(x => x.Dto.StartDate)
                .NotEqual(default(DateTime)).WithMessage("StartDate is required.")
                .MustBeWallClock()
                .MustBeWithinSupportedDates();
            RuleFor(x => x.Dto.EndDate)
                .NotEqual(default(DateTime)).WithMessage("EndDate is required.")
                .GreaterThan(x => x.Dto.StartDate)
                .WithMessage("EndDate must be after StartDate.")
                .MustBeWallClock()
                .MustBeWithinSupportedDates();
            RuleFor(x => x.Dto.Notes).MaximumLength(2000);
            RuleFor(x => x.Dto.Status)
                .Must(s => !s.HasValue || s.Value.IsValidInitialStatus())
                .WithMessage("The initial status can only be Pending or Confirmed.");
            When(x => x.Dto.ExtraServiceIds is { Count: > 0 }, () =>
            {
                RuleForEach(x => x.Dto.ExtraServiceIds).NotEmpty();
                RuleFor(x => x.Dto.ExtraServiceIds!.Count)
                    .LessThanOrEqualTo(10)
                    .WithMessage("No more than 10 extra services can be combined.");
                RuleFor(x => x.Dto)
                    .Must(d => d.ExtraServiceIds!.Distinct().Count() == d.ExtraServiceIds!.Count
                               && !d.ExtraServiceIds!.Contains(d.ServiceId))
                    .WithMessage("Extra services cannot be repeated nor match the primary service.");
            });
        }
    }
}
