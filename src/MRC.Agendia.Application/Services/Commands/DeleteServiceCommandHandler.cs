using MediatR;

namespace MRC.Agendia.Application.Services.Commands
{
    public class DeleteServiceCommandHandler : IRequestHandler<DeleteServiceCommand, bool>
    {
        private readonly IServicesService _service;

        public DeleteServiceCommandHandler(IServicesService service)
        {
            _service = service;
        }

        public async Task<bool> Handle(DeleteServiceCommand request, CancellationToken cancellationToken)
        {
            return await _service.DeleteAsync(request.Id);
        }
    }
}
