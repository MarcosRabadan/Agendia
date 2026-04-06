using MediatR;
using MRC.Agendia.Application.BusinessSchedule.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.BusinessSchedule.Queries
{
    public class GetAllBusinessSchedulesQueryHandler : IRequestHandler<GetAllBusinessSchedulesQuery, IEnumerable<BusinessScheduleDto>>
    {
        private readonly IBusinessScheduleRepository _repository;

        public GetAllBusinessSchedulesQueryHandler(IBusinessScheduleRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<BusinessScheduleDto>> Handle(GetAllBusinessSchedulesQuery request, CancellationToken cancellationToken)
        {
            var entities = await _repository.GetAllAsync();
            return entities.Select(e => new BusinessScheduleDto(e.Id, e.BusinessId, e.DayOfWeek, e.StartTime, e.EndTime, e.IsWorkingDay));
        }
    }
}
