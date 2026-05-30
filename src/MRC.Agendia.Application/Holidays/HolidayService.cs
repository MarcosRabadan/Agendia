using AutoMapper;
using MRC.Agendia.Application.Holidays.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Holidays
{
    public class HolidayService : IHolidayService
    {
        private readonly IHolidayCalendarRepository _repository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public HolidayService(IHolidayCalendarRepository repository, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<HolidayCalendarDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var entities = await _repository.GetAllAsync(cancellationToken);
            return _mapper.Map<IEnumerable<HolidayCalendarDto>>(entities);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<HolidayCalendarDto>> GetByYearAsync(int year, CancellationToken cancellationToken = default)
        {
            var entities = await _repository.GetByYearAsync(year, cancellationToken);
            return _mapper.Map<IEnumerable<HolidayCalendarDto>>(entities);
        }

        /// <inheritdoc />
        public async Task<HolidayCalendarDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken);
            return entity is null ? null : _mapper.Map<HolidayCalendarDto>(entity);
        }

        /// <inheritdoc />
        public async Task<HolidayCalendarDto> CreateAsync(CreateHolidayCalendarDto dto, CancellationToken cancellationToken = default)
        {
            var entity = _mapper.Map<HolidayCalendar>(dto);
            await _repository.AddAsync(entity, cancellationToken);
            await _unitOfWork.Save(cancellationToken);
            return _mapper.Map<HolidayCalendarDto>(entity);
        }

        /// <inheritdoc />
        public async Task<HolidayCalendarDto> UpdateAsync(UpdateHolidayCalendarDto dto, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(dto.Id, cancellationToken)
                ?? throw new HolidayNotFoundException(dto.Id);

            _mapper.Map(dto, entity);
            _repository.Update(entity);
            await _unitOfWork.Save(cancellationToken);
            return _mapper.Map<HolidayCalendarDto>(entity);
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id, cancellationToken)
                ?? throw new HolidayNotFoundException(id);

            _repository.Delete(entity);
            await _unitOfWork.Save(cancellationToken);
            return true;
        }
    }
}
