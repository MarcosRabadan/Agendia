using MediatR;

namespace MRC.Agendia.Application.Business.Commands
{
    public class DeleteBusinessCommandHandler : IRequestHandler<DeleteBusinessCommand, bool>
    {
        private readonly IBusinessService _service;

        public DeleteBusinessCommandHandler(IBusinessService service)
        {
            _service = service;
        }

        public async Task<bool> Handle(DeleteBusinessCommand request, CancellationToken cancellationToken)
        {
            return await _service.DeleteAsync(request.Id, cancellationToken);
        }
    }
}
