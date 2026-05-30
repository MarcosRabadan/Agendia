using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Services.DTO;

namespace MRC.Agendia.Application.Services.Commands.Create
{
    public class CreateServiceCommandHandler : IRequestHandler<CreateServiceCommand, ServiceDto>
    {
        private readonly IServicesService _service;
        private readonly IResourceAuthorizationService _auth;

        public CreateServiceCommandHandler(IServicesService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<ServiceDto> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageBusinessResourcesAsync(request.Dto.BusinessId, cancellationToken);
            return await _service.CreateAsync(request.Dto, cancellationToken);
        }
    }
}
