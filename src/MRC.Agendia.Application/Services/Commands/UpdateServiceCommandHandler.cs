using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Services.DTO;

namespace MRC.Agendia.Application.Services.Commands
{
    public class UpdateServiceCommandHandler : IRequestHandler<UpdateServiceCommand, ServiceDto>
    {
        private readonly IServicesService _service;
        private readonly IResourceAuthorizationService _auth;

        public UpdateServiceCommandHandler(IServicesService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<ServiceDto> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageBusinessResourcesAsync(request.Dto.BusinessId);
            return await _service.UpdateAsync(request.Dto);
        }
    }
}
