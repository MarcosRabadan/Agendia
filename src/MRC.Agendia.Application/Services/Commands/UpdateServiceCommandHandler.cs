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
            // Validate against the EXISTING service (its current business), not the
            // BusinessId carried by the DTO. Otherwise an Owner could PUT a service
            // that belongs to a different tenant, see issue #91. The DTO's BusinessId
            // is additionally ignored by AutoMapper so reassignment is not possible
            // even after passing the check.
            await _auth.EnsureCanManageServiceAsync(request.Dto.Id);
            return await _service.UpdateAsync(request.Dto);
        }
    }
}
