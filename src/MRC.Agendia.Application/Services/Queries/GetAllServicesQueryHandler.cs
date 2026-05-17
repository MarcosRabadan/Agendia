using MediatR;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Services.DTO;

namespace MRC.Agendia.Application.Services.Queries
{
    public class GetAllServicesQueryHandler : IRequestHandler<GetAllServicesQuery, PagedResult<ServiceDto>>
    {
        private readonly IServicesService _service;

        public GetAllServicesQueryHandler(IServicesService service)
        {
            _service = service;
        }

        public Task<PagedResult<ServiceDto>> Handle(GetAllServicesQuery request, CancellationToken cancellationToken)
            => _service.GetPagedAsync(request.Page, request.PageSize);
    }
}
