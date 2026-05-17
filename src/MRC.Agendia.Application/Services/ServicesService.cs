using AutoMapper;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Services
{
    public class ServicesService : IServicesService
    {
        private readonly IServiceRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ServicesService(IServiceRepository repository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        #region CRUD
        public async Task<PagedResult<ServiceDto>> GetPagedAsync(int page, int pageSize)
        {
            var (items, totalCount) = await _repository.GetPagedAsync(page, pageSize);
            var dtos = _mapper.Map<List<ServiceDto>>(items);
            return PagedResult<ServiceDto>.Create(dtos, totalCount, page, pageSize);
        }

        public async Task<ServiceDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity is null ? null : _mapper.Map<ServiceDto>(entity);
        }

        public async Task<ServiceDto> CreateAsync(CreateServiceDto dto)
        {
            var entity = _mapper.Map<Service>(dto);
            await _repository.AddAsync(entity);
            await _unitOfWork.Save();
            return _mapper.Map<ServiceDto>(entity);
        }

        public async Task<ServiceDto> UpdateAsync(UpdateServiceDto dto)
        {
            var entity = await _repository.GetByIdAsync(dto.Id)
                ?? throw new KeyNotFoundException($"Service with Id {dto.Id} not found.");

            _mapper.Map(dto, entity);
            _repository.Update(entity);
            await _unitOfWork.Save();
            return _mapper.Map<ServiceDto>(entity);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Service with Id {id} not found.");

            _repository.Delete(entity);
            await _unitOfWork.Save();
            return true;
        }

        #endregion CRUD
    
        
    }
}
