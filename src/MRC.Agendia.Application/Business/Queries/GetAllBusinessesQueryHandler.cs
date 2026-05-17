using MediatR;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Business.Queries
{
    public class GetAllBusinessesQueryHandler : IRequestHandler<GetAllBusinessesQuery, PagedResult<BusinessDto>>
    {
        private readonly IBusinessService _service;

        public GetAllBusinessesQueryHandler(IBusinessService service)
        {
            _service = service;
        }

        public Task<PagedResult<BusinessDto>> Handle(GetAllBusinessesQuery request, CancellationToken cancellationToken)
            => _service.GetPagedAsync(request.Page, request.PageSize);
    }
}
