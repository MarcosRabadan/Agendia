using MediatR;
using MRC.Agendia.Application.BusinessSchedule.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.BusinessSchedule.Commands
{
    public record CreateBusinessScheduleCommand(CreateBusinessScheduleDto Dto) : IRequest<BusinessScheduleDto>;
    public record UpdateBusinessScheduleCommand(UpdateBusinessScheduleDto Dto) : IRequest<BusinessScheduleDto>;
    public record DeleteBusinessScheduleCommand(int Id) : IRequest<bool>;

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

    public class DeleteBusinessScheduleCommandHandler : IRequestHandler<DeleteBusinessScheduleCommand, bool>
    {
        private readonly IBusinessScheduleRepository _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteBusinessScheduleCommandHandler(IBusinessScheduleRepository repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteBusinessScheduleCommand request, CancellationToken cancellationToken)
        {
            var entity = await _repository.GetByIdAsync(request.Id)
                ?? throw new KeyNotFoundException($"BusinessSchedule with Id {request.Id} not found.");

            _repository.Delete(entity);
            await _unitOfWork.Save();
            return true;
        }
    }
}
