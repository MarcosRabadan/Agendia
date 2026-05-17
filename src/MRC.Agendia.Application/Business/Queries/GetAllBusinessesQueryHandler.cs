using MediatR;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Business.Queries
{
    public class GetAllBusinessesQueryHandler : IRequestHandler<GetAllBusinessesQuery, PagedResult<BusinessPublicDto>>
    {
        private readonly IBusinessService _service;

        public GetAllBusinessesQueryHandler(IBusinessService service)
        {
            _service = service;
        }

        public Task<PagedResult<BusinessPublicDto>> Handle(GetAllBusinessesQuery request, CancellationToken cancellationToken)
            => _service.GetPagedPublicAsync(request.Page, request.PageSize);
    }
}
