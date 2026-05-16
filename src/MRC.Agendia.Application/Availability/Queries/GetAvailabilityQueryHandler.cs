using MediatR;
using MRC.Agendia.Application.Availability.DTO;

namespace MRC.Agendia.Application.Availability.Queries
{
    public class GetAvailabilityQueryHandler : IRequestHandler<GetAvailabilityQuery, AvailabilityDto>
    {
        private readonly IAvailabilityService _service;

        public GetAvailabilityQueryHandler(IAvailabilityService service)
        {
            _service = service;
        }

        public Task<AvailabilityDto> Handle(GetAvailabilityQuery request, CancellationToken cancellationToken)
            => _service.GetAvailabilityAsync(
                request.BusinessId,
                request.Date,
                request.ServiceId,
                request.EmployeeId,
                request.StepMinutes);
    }
}
