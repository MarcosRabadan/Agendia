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
            // Authorize against the EXISTING service (its current business). The DTO
            // no longer carries a BusinessId, so a service cannot be relocated to
            // another tenant on update (see issue #91).
            await _auth.EnsureCanManageServiceAsync(request.Dto.Id, cancellationToken);
            return await _service.UpdateAsync(request.Dto, cancellationToken);
        }
    }
}
