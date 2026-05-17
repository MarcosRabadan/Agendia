using FluentValidation;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Business.Queries
{
    public class GetAllBusinessesQueryValidator : AbstractValidator<GetAllBusinessesQuery>
    {
        public GetAllBusinessesQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, PaginationConstants.MaxPageSize);
        }
    }
}
