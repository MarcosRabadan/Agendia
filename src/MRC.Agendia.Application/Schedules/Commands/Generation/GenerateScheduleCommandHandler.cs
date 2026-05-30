using MediatR;
using MRC.Agendia.Application.Authorization;
using MRC.Agendia.Application.Schedules.DTO;

namespace MRC.Agendia.Application.Schedules.Commands.Generation
{
    public class GenerateScheduleCommandHandler : IRequestHandler<GenerateScheduleCommand, GenerateScheduleResponseDto>
    {
        private readonly IScheduleGenerationService _service;
        private readonly IResourceAuthorizationService _auth;

        public GenerateScheduleCommandHandler(IScheduleGenerationService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<GenerateScheduleResponseDto> Handle(GenerateScheduleCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageBusinessResourcesAsync(request.Dto.BusinessId, cancellationToken);
            return await _service.GenerateScheduleAsync(request.Dto, cancellationToken);
        }
    }
}
