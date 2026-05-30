using MediatR;

namespace MRC.Agendia.Application.Business.Commands.Restore
{
    public class RestoreBusinessCommandHandler : IRequestHandler<RestoreBusinessCommand, bool>
    {
        private readonly IBusinessService _service;

        public RestoreBusinessCommandHandler(IBusinessService service)
        {
            _service = service;
        }

        public async Task<bool> Handle(RestoreBusinessCommand request, CancellationToken cancellationToken)
        {
            return await _service.RestoreAsync(request.Id, cancellationToken);
        }
    }
}
