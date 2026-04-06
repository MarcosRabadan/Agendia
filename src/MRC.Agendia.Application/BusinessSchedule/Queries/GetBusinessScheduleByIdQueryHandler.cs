using MediatR;
using MRC.Agendia.Application.BusinessSchedule.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.BusinessSchedule.Queries
{
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
}
