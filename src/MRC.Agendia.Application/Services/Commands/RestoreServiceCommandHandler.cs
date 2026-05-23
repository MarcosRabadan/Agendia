using MediatR;

namespace MRC.Agendia.Application.Services.Commands
{
    public class RestoreServiceCommandHandler : IRequestHandler<RestoreServiceCommand, bool>
    {
        private readonly IServicesService _service;

        public RestoreServiceCommandHandler(IServicesService service)
        {
            _service = service;
        }

        public async Task<bool> Handle(RestoreServiceCommand request, CancellationToken cancellationToken)
        {
            return await _service.RestoreAsync(request.Id, cancellationToken);
        }
    }
}
