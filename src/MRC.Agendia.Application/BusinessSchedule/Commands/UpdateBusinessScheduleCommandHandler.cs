using MediatR;
using MRC.Agendia.Application.BusinessSchedule.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.BusinessSchedule.Commands
{
    public class UpdateBusinessScheduleCommandHandler : IRequestHandler<UpdateBusinessScheduleCommand, BusinessScheduleDto>
    {
        private readonly IBusinessScheduleRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateBusinessScheduleCommandHandler(IBusinessScheduleRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<BusinessScheduleDto> Handle(UpdateBusinessScheduleCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Dto.Id)
                ?? throw new KeyNotFoundException($"BusinessSchedule with Id {request.Dto.Id} not found.");

            entity.BusinessId = request.Dto.BusinessId;
            entity.DayOfWeek = request.Dto.DayOfWeek;
            entity.StartTime = request.Dto.StartTime;
            entity.EndTime = request.Dto.EndTime;
            entity.IsWorkingDay = request.Dto.IsWorkingDay;

            _repository.Update(entity);
            await _unitOfWork.Save();

            return new BusinessScheduleDto(entity.Id, entity.BusinessId, entity.DayOfWeek, entity.StartTime, entity.EndTime, entity.IsWorkingDay);
        }
    }
}
