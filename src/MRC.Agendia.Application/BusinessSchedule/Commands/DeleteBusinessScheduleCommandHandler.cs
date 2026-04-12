using MediatR;

namespace MRC.Agendia.Application.BusinessSchedule.Commands
{
    public class DeleteBusinessScheduleCommandHandler : IRequestHandler<DeleteBusinessScheduleCommand, bool>
    {
        private readonly IBusinessScheduleService _service;

        public DeleteBusinessScheduleCommandHandler(IBusinessScheduleService service)
        {
            _service = service;
        }

        public async Task<bool> Handle(DeleteBusinessScheduleCommand request, CancellationToken cancellationToken)
        {
            return await _service.DeleteAsync(request.Id);
        }
    }
}
