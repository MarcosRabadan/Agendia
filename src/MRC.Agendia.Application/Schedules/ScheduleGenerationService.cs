using AutoMapper;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Domain.Services;

namespace MRC.Agendia.Application.Schedules
{
    public class ScheduleGenerationService : IScheduleGenerationService
    {
        private readonly IScheduleTemplateRepository _templateRepository;
        private readonly IScheduleOverrideRepository _overrideRepository;
        private readonly IHolidayCalendarRepository _holidayRepository;
        private readonly IScheduleResolver _scheduleResolver;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ScheduleGenerationService(
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

        public async Task<GenerateScheduleResponseDto> GenerateScheduleAsync(GenerateScheduleRequestDto dto)
        {
            var build = await BuildAsync(dto);

            foreach (var template in build.Templates)
                await _templateRepository.AddAsync(template);

            if (build.Overrides.Count > 0)
                await _overrideRepository.AddRangeAsync(build.Overrides);

            await _unitOfWork.Save();

            return new GenerateScheduleResponseDto(
                TemplateIds: build.Templates.Select(t => t.Id).ToList(),
                TotalWorkingDays: build.TotalWorkingDays,
                TotalHolidays: build.HolidayCount,
                TotalVacationDays: build.VacationDays,
                TotalClosedDays: build.CustomClosedDays,
                Warnings: build.Warnings.Count > 0 ? build.Warnings : null);
        }

        public async Task<IEnumerable<CalendarDayDto>> PreviewScheduleAsync(GenerateScheduleRequestDto dto)
        {
            var build = await BuildAsync(dto);

            var yearFrom = new DateOnly(dto.Year, 1, 1);
            var yearTo = new DateOnly(dto.Year, 12, 31);

            // Merge the (unpersisted) request with the business's existing schedule
            // so the preview reflects what the calendar would actually look like.
            var existingTemplates = await _templateRepository.GetByBusinessIdAsync(dto.BusinessId);
            var existingOverrides = await _overrideRepository.GetByBusinessIdAndDateRangeAsync(dto.BusinessId, yearFrom, yearTo);

            var allTemplates = existingTemplates.Concat(build.Templates).ToList();
            var allOverrides = existingOverrides.Concat(build.Overrides).ToList();

            var days = new List<CalendarDayDto>();
            for (var date = yearFrom; date <= yearTo; date = date.AddDays(1))
            {
                var effective = _scheduleResolver.Resolve(allTemplates, allOverrides, date);
                days.Add(new CalendarDayDto(
                    Date: effective.Date,
                    IsOpen: effective.IsOpen,
                    Status: effective.IsOpen ? "Abierto" : effective.ClosedReason ?? "Cerrado",
                    TimeSlots: effective.IsOpen
                        ? effective.TimeSlots.Select(ts => new EffectiveTimeSlotDto(ts.StartTime, ts.EndTime)).ToList()
                        : null));
            }

            return days;
        }

        /// <summary>
        /// Validates the request and builds the templates + overrides in memory
        /// (no persistence). Shared by generate (which then saves) and preview
        /// (which resolves them against the existing schedule).
        /// </summary>
        private async Task<ScheduleBuild> BuildAsync(GenerateScheduleRequestDto dto)
        {
            if (dto.Templates is null || dto.Templates.Count == 0)
                throw new InvalidOperationException("Debe proporcionar al menos una plantilla de horario.");

            // 1. Validate the requested templates do not overlap each other.
            ValidateTemplatesDoNotOverlap(dto.Templates);

            // 2. Validate against templates already in the DB.
            foreach (var templateInput in dto.Templates)
            {
                if (await _templateRepository.HasOverlappingTemplateAsync(dto.BusinessId, templateInput.EffectiveFrom, templateInput.EffectiveTo))
                    throw new TemplatesOverlapException($"La plantilla '{templateInput.Name}' se solapa con una plantilla existente del negocio.");
            }

            var warnings = new List<string>();

            // 3. Build the templates with their slots (not persisted here).
            var templates = new List<ScheduleTemplate>();
            foreach (var templateInput in dto.Templates)
            {
                var template = _mapper.Map<ScheduleTemplate>(templateInput);
                template.BusinessId = dto.BusinessId;
                template.CreatedAt = DateTime.UtcNow;
                templates.Add(template);
            }

            // 4. Year range that covers the holidays/vacations.
            var yearFrom = new DateOnly(dto.Year, 1, 1);
            var yearTo = new DateOnly(dto.Year, 12, 31);

            // 5. Collect overrides.
            var overrides = new List<ScheduleOverride>();
            var holidayDates = new HashSet<DateOnly>();

            // 5a. National/local holidays.
            if (dto.IncludeNationalHolidays || dto.IncludeLocalHolidays)
            {
                var holidays = await _holidayRepository.GetByDateRangeAsync(yearFrom, yearTo);

                foreach (var holiday in holidays)
                {
                    if (holiday.Scope == HolidayScope.National && !dto.IncludeNationalHolidays) continue;
                    if (holiday.Scope != HolidayScope.National && !dto.IncludeLocalHolidays) continue;

                    if (holidayDates.Contains(holiday.Date)) continue; // avoid duplicates

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

            // 5b. Vacation periods.
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

            // 5c. Ad-hoc closures.
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

            // 6. Total working days across all template ranges.
            int totalWorkingDays = 0;
            foreach (var templateInput in dto.Templates)
            {
                var workingDaysOfWeek = templateInput.WeeklySlots
                    .Select(s => s.DayOfWeek)
                    .Distinct()
                    .ToHashSet();

                for (var date = templateInput.EffectiveFrom; date <= templateInput.EffectiveTo; date = date.AddDays(1))
                {
                    if (workingDaysOfWeek.Contains(date.DayOfWeek))
                        totalWorkingDays++;
                }
            }

            return new ScheduleBuild(
                templates, overrides, warnings,
                holidayDates.Count, vacationDays, customClosedDays, totalWorkingDays);
        }

        private static void ValidateTemplatesDoNotOverlap(List<GenerateScheduleTemplateInputDto> templates)
        {
            for (int i = 0; i < templates.Count; i++)
            {
                if (templates[i].EffectiveFrom > templates[i].EffectiveTo)
                    throw new InvalidOperationException($"La plantilla '{templates[i].Name}' tiene una fecha de inicio posterior a la de fin.");

                for (int j = i + 1; j < templates.Count; j++)
                {
                    if (templates[i].EffectiveFrom <= templates[j].EffectiveTo
                        && templates[i].EffectiveTo >= templates[j].EffectiveFrom)
                    {
                        throw new TemplatesOverlapException(
                            $"Las plantillas '{templates[i].Name}' y '{templates[j].Name}' tienen fechas que se solapan.");
                    }
                }
            }
        }

        private sealed record ScheduleBuild(
            List<ScheduleTemplate> Templates,
            List<ScheduleOverride> Overrides,
            List<string> Warnings,
            int HolidayCount,
            int VacationDays,
            int CustomClosedDays,
            int TotalWorkingDays);
    }
}
