using MediatR;
using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Clients.Queries
{
    public class GetAllClientsQueryHandler : IRequestHandler<GetAllClientsQuery, PagedResult<ClientDto>>
    {
        private readonly IClientService _service;

        public GetAllClientsQueryHandler(IClientService service)
        {
            _service = service;
        }

        public Task<PagedResult<ClientDto>> Handle(GetAllClientsQuery request, CancellationToken cancellationToken)
            => _service.GetPagedAsync(request.Page, request.PageSize);
    }
}
