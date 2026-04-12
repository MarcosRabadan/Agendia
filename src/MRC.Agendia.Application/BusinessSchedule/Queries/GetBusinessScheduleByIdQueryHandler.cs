using MediatR;
using MRC.Agendia.Application.BusinessSchedule.DTO;

namespace MRC.Agendia.Application.BusinessSchedule.Queries
{
    public class GetBusinessScheduleByIdQueryHandler : IRequestHandler<GetBusinessScheduleByIdQuery, BusinessScheduleDto?>
    {
        private readonly IBusinessScheduleService _service;

        public GetBusinessScheduleByIdQueryHandler(IBusinessScheduleService service)
        {
            _service = service;
        }

        public async Task<BusinessScheduleDto?> Handle(GetBusinessScheduleByIdQuery request, CancellationToken cancellationToken)
        {
            return await _service.GetByIdAsync(request.Id);
        }
    }
}
