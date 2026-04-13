using AutoMapper;
using MRC.Agendia.Application.Holidays.DTO;
using MRC.Agendia.Domain.Entities;
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

        public async Task<IEnumerable<HolidayCalendarDto>> GetAllAsync()
        {
            var entities = await _repository.GetAllAsync();
            return _mapper.Map<IEnumerable<HolidayCalendarDto>>(entities);
        }

        public async Task<IEnumerable<HolidayCalendarDto>> GetByYearAsync(int year, string? region)
        {
            var entities = await _repository.GetByYearAndRegionAsync(year, region);
            return _mapper.Map<IEnumerable<HolidayCalendarDto>>(entities);
        }

        public async Task<HolidayCalendarDto?> GetByIdAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            return entity is null ? null : _mapper.Map<HolidayCalendarDto>(entity);
        }

        public async Task<HolidayCalendarDto> CreateAsync(CreateHolidayCalendarDto dto)
        {
            var entity = _mapper.Map<HolidayCalendar>(dto);
            await _repository.AddAsync(entity);
            await _unitOfWork.Save();
            return _mapper.Map<HolidayCalendarDto>(entity);
        }

        public async Task<HolidayCalendarDto> UpdateAsync(UpdateHolidayCalendarDto dto)
        {
            var entity = await _repository.GetByIdAsync(dto.Id)
                ?? throw new KeyNotFoundException($"Holiday with Id {dto.Id} not found.");

            _mapper.Map(dto, entity);
            _repository.Update(entity);
            await _unitOfWork.Save();
            return _mapper.Map<HolidayCalendarDto>(entity);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Holiday with Id {id} not found.");

            _repository.Delete(entity);
            await _unitOfWork.Save();
            return true;
        }
    }
}
