using AutoMapper;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Exceptions;
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
        /// <inheritdoc />
        public async Task<PagedResult<ServiceDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _repository.GetPagedAsync(page, pageSize, cancellationToken);
            var dtos = _mapper.Map<List<ServiceDto>>(items);
            return PagedResult<ServiceDto>.Create(dtos, totalCount, page, pageSize);
        }

        /// <inheritdoc />
        public async Task<ServiceDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            // Public catalog detail (GET /api/Service/{id} is [AllowAnonymous]):
            // read unscoped so an authenticated owner/employee can see any service,
            // not only their own business's (#58). Update/Delete stay scoped.
            var entity = await _repository.GetByIdPublicAsync(id, cancellationToken);
            return entity is null ? null : _mapper.Map<ServiceDto>(entity);
        }

        /// <inheritdoc />
        public async Task<ServiceDto> CreateAsync(CreateServiceDto dto, CancellationToken cancellationToken = default)
        {
            var entity = _mapper.Map<Service>(dto);
            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.Save(cancellationToken);
            return _mapper.Map<ServiceDto>(entity);
        }

        /// <inheritdoc />
        public async Task<ServiceDto> UpdateAsync(UpdateServiceDto dto, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(dto.Id, cancellationToken)
                ?? throw new ServiceNotFoundException(dto.Id);

            _mapper.Map(dto, entity);
            _repository.Update(entity);
            await _unitOfWork.Save(cancellationToken);
            return _mapper.Map<ServiceDto>(entity);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new ServiceNotFoundException(id);

            _repository.Delete(entity);
            await _unitOfWork.Save(cancellationToken);
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdIncludingDeletedAsync(id, cancellationToken)
                ?? throw new ServiceNotFoundException(id);

            if (!entity.IsDeleted) return true;

            entity.IsDeleted = false;
            entity.DeletedAt = null;
            _repository.Update(entity);
            await _unitOfWork.Save(cancellationToken);
            return true;
        }

        #endregion CRUD


    }
}
