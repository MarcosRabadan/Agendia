using MediatR;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands
{
    public class GenerateScheduleCommandHandler : IRequestHandler<GenerateScheduleCommand, GenerateScheduleResponseDto>
    {
        private readonly IScheduleService _service;

        public GenerateScheduleCommandHandler(IScheduleService service)
        {
            _service = service;
        }

        public async Task<GenerateScheduleResponseDto> Handle(GenerateScheduleCommand request, CancellationToken cancellationToken)
        {
            return await _service.GenerateScheduleAsync(request.Dto);
        }
    }
}
