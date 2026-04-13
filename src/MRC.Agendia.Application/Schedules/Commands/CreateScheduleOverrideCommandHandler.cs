using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public class CreateScheduleOverrideCommandHandler : IRequestHandler<CreateScheduleOverrideCommand, ScheduleOverrideDto>
    {
        private readonly IScheduleService _service;

        public CreateScheduleOverrideCommandHandler(IScheduleService service)
        {
            _service = service;
        }

        public async Task<ScheduleOverrideDto> Handle(CreateScheduleOverrideCommand request, CancellationToken cancellationToken)
        {
            return await _service.CreateOverrideAsync(request.Dto);
        }
    }
}
