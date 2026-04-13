using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public class UpdateScheduleOverrideCommandHandler : IRequestHandler<UpdateScheduleOverrideCommand, ScheduleOverrideDto>
    {
        private readonly IScheduleService _service;

        public UpdateScheduleOverrideCommandHandler(IScheduleService service)
        {
            _service = service;
        }

        public async Task<ScheduleOverrideDto> Handle(UpdateScheduleOverrideCommand request, CancellationToken cancellationToken)
        {
            return await _service.UpdateOverrideAsync(request.Dto);
        }
    }
}
