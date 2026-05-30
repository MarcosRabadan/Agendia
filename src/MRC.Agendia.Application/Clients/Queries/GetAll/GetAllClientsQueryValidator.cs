using FluentValidation;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Clients.Queries.GetAll
{
    public class GetAllClientsQueryValidator : AbstractValidator<GetAllClientsQuery>
    {
        public GetAllClientsQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, PaginationConstants.MaxPageSize);
        }
    }
}
