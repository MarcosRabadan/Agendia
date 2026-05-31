using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Application.Common;

namespace MRC.Agendia.Application.Clients.Queries.GetByBusiness
{
    public class GetBusinessClientsQueryHandler : IRequestHandler<GetBusinessClientsQuery, PagedResult<ClientDto>>
    {
        private readonly IClientService _service;
        private readonly IResourceAuthorizationService _auth;

        public GetBusinessClientsQueryHandler(IClientService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<PagedResult<ClientDto>> Handle(GetBusinessClientsQuery request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageBusinessResourcesAsync(request.BusinessId, cancellationToken);
            return await _service.GetPagedByBusinessAsync(request.BusinessId, request.Page, request.PageSize, cancellationToken);
        }
    }
}
