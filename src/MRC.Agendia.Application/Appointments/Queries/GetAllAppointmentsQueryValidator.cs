using FluentValidation;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Appointments.Queries
{
    public class GetAllAppointmentsQueryValidator : AbstractValidator<GetAllAppointmentsQuery>
    {
        public GetAllAppointmentsQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, PaginationConstants.MaxPageSize);
        }
    }
}
