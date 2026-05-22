using FluentValidation;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Appointments.Queries
{
    public class GetMyAppointmentsAsClientQueryValidator : AbstractValidator<GetMyAppointmentsAsClientQuery>
    {
        public GetMyAppointmentsAsClientQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, PaginationConstants.MaxPageSize);
        }
    }
}
