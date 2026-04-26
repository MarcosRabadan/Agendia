using AutoMapper;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
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

        #region Generate
        public async Task<GenerateScheduleResponseDto> GenerateScheduleAsync(GenerateScheduleRequestDto dto)
        {
            if (await _templateRepository.HasOverlappingTemplateAsync(dto.BusinessId, dto.EffectiveFrom, dto.EffectiveTo))
                throw new InvalidOperationException("Ya existe una plantilla de horario que se solapa con las fechas indicadas.");

            var warnings = new List<string>();

            // 1. Create template
            var template = _mapper.Map<ScheduleTemplate>(dto);
            template.CreatedAt = DateTime.UtcNow;
            await _templateRepository.AddAsync(template);

            // 2. Collect overrides
            var overrides = new List<ScheduleOverride>();
            var holidayDates = new HashSet<DateOnly>();

            // 2a. National/Local holidays
            if (dto.IncludeNationalHolidays || dto.IncludeLocalHolidays)
            {
                var holidays = await _holidayRepository.GetByDateRangeAsync(dto.EffectiveFrom, dto.EffectiveTo, dto.Region);

                foreach (var holiday in holidays)
                {
                    if (holiday.Scope == HolidayScope.National && !dto.IncludeNationalHolidays) continue;
                    if ((holiday.Scope == HolidayScope.Regional || holiday.Scope == HolidayScope.Local) && !dto.IncludeLocalHolidays) continue;

                    holidayDates.Add(holiday.Date);
                    overrides.Add(new ScheduleOverride
                    {
                        BusinessId = dto.BusinessId,
                        Date = holiday.Date,
                        OverrideType = holiday.Scope == HolidayScope.National
                            ? ScheduleOverrideType.NationalHoliday
                            : ScheduleOverrideType.LocalHoliday,
                        Reason = holiday.Name,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // 2b. Vacation periods
            int vacationDays = 0;
            if (dto.VacationPeriods != null)
            {
                foreach (var vacation in dto.VacationPeriods)
                {
                    for (var date = vacation.From; date <= vacation.To; date = date.AddDays(1))
                    {
                        if (holidayDates.Contains(date))
                        {
                            warnings.Add($"El dia {date:yyyy-MM-dd} es festivo y tambien esta marcado como vacaciones ({vacation.Reason ?? "sin motivo"}).");
                            continue;
                        }

                        vacationDays++;
                        overrides.Add(new ScheduleOverride
                        {
                            BusinessId = dto.BusinessId,
                            Date = date,
                            OverrideType = ScheduleOverrideType.Closed,
                            Reason = vacation.Reason ?? "Vacaciones",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }

            // 2c. Custom closed dates
            int customClosedDays = 0;
            if (dto.CustomClosedDates != null)
            {
                foreach (var closed in dto.CustomClosedDates)
                {
                    if (holidayDates.Contains(closed.Date))
                    {
                        warnings.Add($"El dia {closed.Date:yyyy-MM-dd} es festivo y tambien esta marcado como cerrado.");
                        continue;
                    }

                    customClosedDays++;
                    overrides.Add(new ScheduleOverride
                    {
                        BusinessId = dto.BusinessId,
                        Date = closed.Date,
                        OverrideType = ScheduleOverrideType.Closed,
                        Reason = closed.Reason ?? "Cierre puntual",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            if (overrides.Count > 0)
                await _overrideRepository.AddRangeAsync(overrides);

            await _unitOfWork.Save();

            // Calculate working days
            var workingDaysOfWeek = dto.WeeklySlots
                .Select(s => s.DayOfWeek)
                .Distinct()
                .ToHashSet();

            int totalWorkingDays = 0;
            for (var date = dto.EffectiveFrom; date <= dto.EffectiveTo; date = date.AddDays(1))
            {
                if (workingDaysOfWeek.Contains(date.DayOfWeek))
                    totalWorkingDays++;
            }

            return new GenerateScheduleResponseDto(
                TemplateId: template.Id,
                TotalWorkingDays: totalWorkingDays,
                TotalHolidays: holidayDates.Count,
                TotalVacationDays: vacationDays,
                TotalClosedDays: customClosedDays,
                Warnings: warnings.Count > 0 ? warnings : null);
        }
        #endregion

        #region Effective Schedule
        public async Task<EffectiveScheduleDto> GetEffectiveScheduleAsync(int businessId, DateOnly date)
        {
            var effective = await _scheduleResolver.GetEffectiveScheduleAsync(businessId, date);
            return _mapper.Map<EffectiveScheduleDto>(effective);
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
