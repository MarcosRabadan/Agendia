using FluentValidation;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Services.Queries.GetAll
{
    public class GetAllServicesQueryValidator : AbstractValidator<GetAllServicesQuery>
    {
        public GetAllServicesQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, PaginationConstants.MaxPageSize);
        }
    }
}
