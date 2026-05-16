using MediatR;
using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Application.Services.Commands
{
    public class DeleteServiceCommandHandler : IRequestHandler<DeleteServiceCommand, bool>
    {
        private readonly IServicesService _service;
        private readonly IResourceAuthorizationService _auth;

        public DeleteServiceCommandHandler(IServicesService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<bool> Handle(DeleteServiceCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageServiceAsync(request.Id);
            return await _service.DeleteAsync(request.Id);
        }
    }
}
