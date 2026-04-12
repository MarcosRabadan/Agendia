using MediatR;
using MRC.Agendia.Application.BusinessSchedule.DTO;

namespace MRC.Agendia.Application.BusinessSchedule.Queries
{
    public class GetAllBusinessSchedulesQueryHandler : IRequestHandler<GetAllBusinessSchedulesQuery, IEnumerable<BusinessScheduleDto>>
    {
        private readonly IBusinessScheduleService _service;

        public GetAllBusinessSchedulesQueryHandler(IBusinessScheduleService service)
        {
            _service = service;
        }

        public async Task<IEnumerable<BusinessScheduleDto>> Handle(GetAllBusinessSchedulesQuery request, CancellationToken cancellationToken)
        {
            return await _service.GetAllAsync();
        }
    }
}
