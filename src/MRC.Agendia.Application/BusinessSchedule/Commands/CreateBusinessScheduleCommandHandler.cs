using MediatR;
using MRC.Agendia.Application.BusinessSchedule.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.BusinessSchedule.Commands
{
    public class CreateBusinessScheduleCommandHandler : IRequestHandler<CreateBusinessScheduleCommand, BusinessScheduleDto>
    {
        private readonly IBusinessScheduleRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateBusinessScheduleCommandHandler(IBusinessScheduleRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<BusinessScheduleDto> Handle(CreateBusinessScheduleCommand request, CancellationToken cancellationToken)
        {
            var entity = new Domain.Entities.BusinessSchedule
            {
                BusinessId = request.Dto.BusinessId,
                DayOfWeek = request.Dto.DayOfWeek,
                StartTime = request.Dto.StartTime,
                EndTime = request.Dto.EndTime,
                IsWorkingDay = request.Dto.IsWorkingDay
            };

            await _repository.AddAsync(entity);
            await _unitOfWork.Save();

            return new BusinessScheduleDto(entity.Id, entity.BusinessId, entity.DayOfWeek, entity.StartTime, entity.EndTime, entity.IsWorkingDay);
        }
    }
}
