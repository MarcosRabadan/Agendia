using FluentValidation;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Services.Queries
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
