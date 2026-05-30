using AutoMapper;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Business
{
    public class BusinessService : IBusinessService
    {
        private readonly IBusinessRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public BusinessService(IBusinessRepository repository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        #region CRUD
        /// <inheritdoc />
        public async Task<PagedResult<BusinessPublicDto>> GetPagedPublicAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var (items, totalCount) = await _repository.GetPagedActiveAsync(page, pageSize, cancellationToken);
            var dtos = _mapper.Map<List<BusinessPublicDto>>(items);
            return PagedResult<BusinessPublicDto>.Create(dtos, totalCount, page, pageSize);
        }

        /// <inheritdoc />
        public async Task<BusinessPublicDto?> GetPublicByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetActiveByIdAsync(id, cancellationToken);
            return entity is null ? null : _mapper.Map<BusinessPublicDto>(entity);
        }

        /// <inheritdoc />
        public async Task<BusinessDto> CreateAsync(CreateBusinessDto dto, CancellationToken cancellationToken = default)
        {
            var entity = _mapper.Map<Domain.Entities.Business>(dto);
            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.Save(cancellationToken);
            return _mapper.Map<BusinessDto>(entity);
        }

        /// <inheritdoc />
        public async Task<BusinessDto> UpdateAsync(UpdateBusinessDto dto, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(dto.Id, cancellationToken)
                ?? throw new BusinessNotFoundException(dto.Id);

            _mapper.Map(dto, entity);
            _repository.Update(entity);
            await _unitOfWork.Save(cancellationToken);
            return _mapper.Map<BusinessDto>(entity);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new BusinessNotFoundException(id);

            _repository.Delete(entity);
            await _unitOfWork.Save(cancellationToken);
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> RestoreAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdIncludingDeletedAsync(id, cancellationToken)
                ?? throw new BusinessNotFoundException(id);

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
