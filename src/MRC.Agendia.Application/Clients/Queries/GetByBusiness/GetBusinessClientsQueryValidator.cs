using FluentValidation;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Clients.Queries.GetByBusiness
{
    public class GetBusinessClientsQueryValidator : AbstractValidator<GetBusinessClientsQuery>
    {
        public GetBusinessClientsQueryValidator()
        {
            RuleFor(x => x.BusinessId).GreaterThan(0);
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, PaginationConstants.MaxPageSize);
        }
    }
}
