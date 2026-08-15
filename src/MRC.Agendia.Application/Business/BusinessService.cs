using AutoMapper;
using MRC.Agendia.Application.Business.DTO;
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
        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new BusinessNotFoundException(id);

            _repository.Delete(entity);
            await _unitOfWork.Save(cancellationToken);
            return true;
        }

        /// <inheritdoc />
        public async Task<bool> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
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
