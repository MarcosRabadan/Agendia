using MediatR;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Authorization;

namespace MRC.Agendia.Application.Appointments.Commands.Delay
{
    public class NotifyDelayCommandHandler : IRequestHandler<NotifyDelayCommand, DelayNotificationResultDto>
    {
        private readonly IAppointmentDelayService _service;
        private readonly IResourceAuthorizationService _auth;

        public NotifyDelayCommandHandler(IAppointmentDelayService service, IResourceAuthorizationService auth)
        {
            _service = service;
            _auth = auth;
        }

        public async Task<DelayNotificationResultDto> Handle(NotifyDelayCommand request, CancellationToken cancellationToken)
        {
            await _auth.EnsureCanManageBusinessResourcesAsync(request.BusinessId, cancellationToken);
            return await _service.NotifyDelayAsync(request.BusinessId, request.Dto, cancellationToken);
        }
    }
}
