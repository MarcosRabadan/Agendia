using MediatR;
using MRC.Agendia.Application.BusinessSchedule.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.BusinessSchedule.Queries
{
    public record GetBusinessScheduleByIdQuery(int Id) : IRequest<BusinessScheduleDto?>;
    public record GetAllBusinessSchedulesQuery() : IRequest<IEnumerable<BusinessScheduleDto>>;

    public class GetBusinessScheduleByIdQueryHandler : IRequestHandler<GetBusinessScheduleByIdQuery, BusinessScheduleDto?>
    {
        private readonly IBusinessScheduleRepository _repository;

        public GetBusinessScheduleByIdQueryHandler(IBusinessScheduleRepository repository)
        {
            _repository = repository;
        }

        public async Task<BusinessScheduleDto?> Handle(GetBusinessScheduleByIdQuery request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id);
            if (entity is null) return null;

            return new BusinessScheduleDto(entity.Id, entity.BusinessId, entity.DayOfWeek, entity.StartTime, entity.EndTime, entity.IsWorkingDay);
        }
    }

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
