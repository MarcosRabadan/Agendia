using AutoMapper;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Domain.Services;

namespace MRC.Agendia.Application.Schedules
{
    public class ScheduleService : IScheduleService
    {
        private readonly IScheduleTemplateRepository _templateRepository;
        private readonly IScheduleOverrideRepository _overrideRepository;
        private readonly IHolidayCalendarRepository _holidayRepository;
        private readonly IScheduleResolver _scheduleResolver;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ScheduleService(
            IScheduleTemplateRepository templateRepository,
            IScheduleOverrideRepository overrideRepository,
            IHolidayCalendarRepository holidayRepository,
            IScheduleResolver scheduleResolver,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _templateRepository = templateRepository;
            _overrideRepository = overrideRepository;
            _holidayRepository = holidayRepository;
            _scheduleResolver = scheduleResolver;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        #region Templates
        public async Task<IEnumerable<ScheduleTemplateDto>> GetTemplatesByBusinessIdAsync(int businessId)
        {
            var entities = await _templateRepository.GetByBusinessIdAsync(businessId);
            return _mapper.Map<IEnumerable<ScheduleTemplateDto>>(entities);
        }

        public async Task<ScheduleTemplateDto?> GetTemplateByIdAsync(int id)
        {
            var entity = await _templateRepository.GetByIdWithSlotsAsync(id);
            return entity is null ? null : _mapper.Map<ScheduleTemplateDto>(entity);
        }

        public async Task<ScheduleTemplateDto> CreateTemplateAsync(CreateScheduleTemplateDto dto)
        {
            if (await _templateRepository.HasOverlappingTemplateAsync(dto.BusinessId, dto.EffectiveFrom, dto.EffectiveTo))
                throw new InvalidOperationException("Ya existe una plantilla de horario que se solapa con las fechas indicadas.");

            var entity = _mapper.Map<ScheduleTemplate>(dto);
            entity.CreatedAt = DateTime.UtcNow;

            await _templateRepository.AddAsync(entity);
            await _unitOfWork.Save();
            return _mapper.Map<ScheduleTemplateDto>(entity);
        }

        public async Task<ScheduleTemplateDto> UpdateTemplateAsync(UpdateScheduleTemplateDto dto)
        {
            var entity = await _templateRepository.GetByIdWithSlotsAsync(dto.Id)
                ?? throw new KeyNotFoundException($"ScheduleTemplate with Id {dto.Id} not found.");

            if (await _templateRepository.HasOverlappingTemplateAsync(entity.BusinessId, dto.EffectiveFrom, dto.EffectiveTo, dto.Id))
                throw new InvalidOperationException("Ya existe una plantilla de horario que se solapa con las fechas indicadas.");

            entity.Name = dto.Name;
            entity.EffectiveFrom = dto.EffectiveFrom;
            entity.EffectiveTo = dto.EffectiveTo;
            entity.IsDefault = dto.IsDefault;

            entity.WeeklySlots.Clear();
            foreach (var slotDto in dto.WeeklySlots)
            {
                entity.WeeklySlots.Add(_mapper.Map<WeeklyTimeSlot>(slotDto));
            }

            _templateRepository.Update(entity);
            await _unitOfWork.Save();
            return _mapper.Map<ScheduleTemplateDto>(entity);
        }

        public async Task<bool> DeleteTemplateAsync(int id)
        {
            var entity = await _templateRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"ScheduleTemplate with Id {id} not found.");

            _templateRepository.Delete(entity);
            await _unitOfWork.Save();
            return true;
        }
        #endregion

        #region Overrides
        public async Task<IEnumerable<ScheduleOverrideDto>> GetOverridesByBusinessIdAsync(int businessId, DateOnly? from, DateOnly? to)
        {
            IEnumerable<ScheduleOverride> entities;

            if (from.HasValue && to.HasValue)
                entities = await _overrideRepository.GetByBusinessIdAndDateRangeAsync(businessId, from.Value, to.Value);
            else
                entities = await _overrideRepository.GetByBusinessIdAsync(businessId);

            return _mapper.Map<IEnumerable<ScheduleOverrideDto>>(entities);
        }

        public async Task<ScheduleOverrideDto?> GetOverrideByIdAsync(int id)
        {
            var entity = await _overrideRepository.GetByIdWithSlotsAsync(id);
            return entity is null ? null : _mapper.Map<ScheduleOverrideDto>(entity);
        }

        public async Task<ScheduleOverrideDto> CreateOverrideAsync(CreateScheduleOverrideDto dto)
        {
            var entity = _mapper.Map<ScheduleOverride>(dto);
            entity.CreatedAt = DateTime.UtcNow;

            await _overrideRepository.AddAsync(entity);
            await _unitOfWork.Save();
            return _mapper.Map<ScheduleOverrideDto>(entity);
        }

        public async Task<ScheduleOverrideDto> UpdateOverrideAsync(UpdateScheduleOverrideDto dto)
        {
            var entity = await _overrideRepository.GetByIdWithSlotsAsync(dto.Id)
                ?? throw new KeyNotFoundException($"ScheduleOverride with Id {dto.Id} not found.");

            entity.Date = dto.Date;
            entity.OverrideType = dto.OverrideType;
            entity.Reason = dto.Reason;

            entity.CustomSlots.Clear();
            if (dto.CustomSlots != null)
            {
                foreach (var slotDto in dto.CustomSlots)
                {
                    entity.CustomSlots.Add(_mapper.Map<CustomTimeSlot>(slotDto));
                }
            }

            _overrideRepository.Update(entity);
            await _unitOfWork.Save();
            return _mapper.Map<ScheduleOverrideDto>(entity);
        }

        public async Task<bool> DeleteOverrideAsync(int id)
        {
            var entity = await _overrideRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"ScheduleOverride with Id {id} not found.");

            _overrideRepository.Delete(entity);
            await _unitOfWork.Save();
            return true;
        }
        #endregion

        #region Effective Schedule
        public async Task<EffectiveScheduleDto> GetEffectiveScheduleAsync(int businessId, DateOnly date)
        {
            var effective = await _scheduleResolver.GetEffectiveScheduleAsync(businessId, date);
            var templateEntities = await _templateRepository.GetByBusinessIdAsync(businessId);
            var templates = _mapper.Map<List<ScheduleTemplateDto>>(templateEntities);
            var activeTemplate = templates.FirstOrDefault(t => t.EffectiveFrom <= date && t.EffectiveTo >= date);

            return new EffectiveScheduleDto(
                Date: effective.Date,
                IsOpen: effective.IsOpen,
                ClosedReason: effective.ClosedReason,
                OverrideType: effective.OverrideType,
                TimeSlots: effective.TimeSlots
                    .Select(ts => new EffectiveTimeSlotDto(ts.StartTime, ts.EndTime))
                    .ToList(),
                ActiveTemplate: activeTemplate,
                Templates: templates);
        }

        public async Task<IEnumerable<CalendarDayDto>> GetCalendarAsync(int businessId, DateOnly from, DateOnly to)
        {
            var days = await _scheduleResolver.GetEffectiveSchedulesAsync(businessId, from, to);
            return days.Select(d => new CalendarDayDto(
                Date: d.Date,
                IsOpen: d.IsOpen,
                Status: d.IsOpen ? "Abierto" : d.ClosedReason ?? "Cerrado",
                TimeSlots: d.IsOpen
                    ? d.TimeSlots.Select(ts => new EffectiveTimeSlotDto(ts.StartTime, ts.EndTime)).ToList()
                    : null
            ));
        }
        #endregion
    }
}
