using FluentValidation;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Appointments.Commands.Crud
{
    public class UpdateAppointmentCommandValidator : AbstractValidator<UpdateAppointmentCommand>
    {
        public UpdateAppointmentCommandValidator()
        {
            RuleFor(x => x.Dto).NotNull();
            RuleFor(x => x.Dto.Id).NotEmpty();
            RuleFor(x => x.Dto.ClientUserId).NotEmpty();
            RuleFor(x => x.Dto.EmployeeId).NotEmpty();
            RuleFor(x => x.Dto.ServiceId).NotEmpty();
            RuleFor(x => x.Dto.StartDate)
                .NotEqual(default(DateTime))
                .MustBeWallClock()
                .MustBeWithinSupportedDates();
            RuleFor(x => x.Dto.EndDate)
                .NotEqual(default(DateTime))
                .GreaterThan(x => x.Dto.StartDate)
                .WithMessage("EndDate must be after StartDate.")
                .MustBeWallClock()
                .MustBeWithinSupportedDates();
            RuleFor(x => x.Dto.Status).IsInEnum();
            RuleFor(x => x.Dto.Notes).MaximumLength(2000);
        }
    }
}
