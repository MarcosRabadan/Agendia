using AutoMapper;
using MRC.Agendia.Application.BusinessSchedule.DTO;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.BusinessSchedule
{
    public class BusinessScheduleService : IBusinessScheduleService
    {
        private readonly IBusinessScheduleRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public BusinessScheduleService(IBusinessScheduleRepository repository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        #region CRUD
        public async Task<IEnumerable<BusinessScheduleDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<BusinessScheduleDto>>(entities);
        }

        public async Task<BusinessScheduleDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity is null ? null : _mapper.Map<BusinessScheduleDto>(entity);
        }

        public async Task<BusinessScheduleDto> CreateAsync(CreateBusinessScheduleDto dto)
        {
            var entity = _mapper.Map<Domain.Entities.BusinessSchedule>(dto);
            await _repository.AddAsync(entity);
            await _unitOfWork.Save();
            return _mapper.Map<BusinessScheduleDto>(entity);
        }

        public async Task<BusinessScheduleDto> UpdateAsync(UpdateBusinessScheduleDto dto)
        {
            var entity = await _repository.GetByIdAsync(dto.Id)
                ?? throw new KeyNotFoundException($"BusinessSchedule with Id {dto.Id} not found.");

            _mapper.Map(dto, entity);
            _repository.Update(entity);
            await _unitOfWork.Save();
            return _mapper.Map<BusinessScheduleDto>(entity);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"BusinessSchedule with Id {id} not found.");

            _repository.Delete(entity);
            await _unitOfWork.Save();
            return true;
        }
        #endregion CRUD
    }
}
