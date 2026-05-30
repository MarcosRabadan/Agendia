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

        /// <inheritdoc />
        public async Task<GenerateScheduleResponseDto> GenerateScheduleAsync(GenerateScheduleRequestDto dto, CancellationToken cancellationToken = default)
        {
            var build = await BuildAsync(dto, cancellationToken);

            foreach (var template in build.Templates)
                await _templateRepository.AddAsync(template, cancellationToken);

            if (build.Overrides.Count > 0)
                await _overrideRepository.AddRangeAsync(build.Overrides, cancellationToken);

            await _unitOfWork.Save(cancellationToken);

            return new GenerateScheduleResponseDto(
                TemplateIds: build.Templates.Select(t => t.Id).ToList(),
                TotalWorkingDays: build.TotalWorkingDays,
                TotalHolidays: build.HolidayCount,
                TotalVacationDays: build.VacationDays,
                TotalClosedDays: build.CustomClosedDays,
                Warnings: build.Warnings.Count > 0 ? build.Warnings : null);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<CalendarDayDto>> PreviewScheduleAsync(GenerateScheduleRequestDto dto, CancellationToken cancellationToken = default)
        {
            var build = await BuildAsync(dto, cancellationToken);

            var yearFrom = new DateOnly(dto.Year, 1, 1);
            var yearTo = new DateOnly(dto.Year, 12, 31);

            // Merge the (unpersisted) request with the business's existing schedule
            // so the preview reflects what the calendar would actually look like.
            var existingTemplates = await _templateRepository.GetByBusinessIdAsync(dto.BusinessId, cancellationToken);
            var existingOverrides = await _overrideRepository.GetByBusinessIdAndDateRangeAsync(dto.BusinessId, yearFrom, yearTo, cancellationToken);

            var allTemplates = existingTemplates.Concat(build.Templates).ToList();
            var allOverrides = existingOverrides.Concat(build.Overrides).ToList();

            return _scheduleResolver
                .ResolveRange(allTemplates, allOverrides, yearFrom, yearTo)
                .Select(CalendarDayDto.FromEffective)
                .ToList();
        }

        /// <summary>
        /// Validates the request and builds the templates + overrides in memory
        /// (no persistence). Shared by generate (which then saves) and preview
        /// (which resolves them against the existing schedule).
        /// </summary>
        private async Task<ScheduleBuild> BuildAsync(GenerateScheduleRequestDto dto, CancellationToken cancellationToken = default)
        {
            if (dto.Templates is null || dto.Templates.Count == 0)
                throw new InvalidOperationException("Debe proporcionar al menos una plantilla de horario.");

            // 1. Validate the requested templates do not overlap each other.
            ValidateTemplatesDoNotOverlap(dto.Templates);

            // 2. Validate against templates already in the DB.
            foreach (var templateInput in dto.Templates)
            {
                if (await _templateRepository.HasOverlappingTemplateAsync(dto.BusinessId, templateInput.EffectiveFrom, templateInput.EffectiveTo, cancellationToken: cancellationToken))
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

            // 5. Collect overrides, deduped by date across holidays + vacations +
            // closures AND against the overrides already persisted for the year, so
            // re-generating (or generating over a manual override) never produces two
            // overrides for the same (BusinessId, Date) - the unique index
            // IX_ScheduleOverride_BusinessId_Date would otherwise throw (HTTP 500).
            var overrides = new List<ScheduleOverride>();
            var existingOverrides = await _overrideRepository.GetByBusinessIdAndDateRangeAsync(dto.BusinessId, yearFrom, yearTo, cancellationToken);
            var claimedDates = new HashSet<DateOnly>(existingOverrides.Select(o => o.Date));
            int holidayCount = 0;

            // 5a. National/local holidays.
            if (dto.IncludeNationalHolidays || dto.IncludeLocalHolidays)
            {
                var holidays = await _holidayRepository.GetByDateRangeAsync(yearFrom, yearTo, cancellationToken);

                foreach (var holiday in holidays)
                {
                    if (holiday.Scope == HolidayScope.National && !dto.IncludeNationalHolidays) continue;
                    if (holiday.Scope != HolidayScope.National && !dto.IncludeLocalHolidays) continue;

                    if (!claimedDates.Add(holiday.Date)) continue; // skip dates already claimed (existing override or earlier holiday)

                    holidayCount++;
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
                        if (!claimedDates.Add(date))
                        {
                            warnings.Add($"El dia {date:yyyy-MM-dd} ya tiene un cierre/festivo; se omite de las vacaciones ({vacation.Reason ?? "sin motivo"}).");
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
                    if (!claimedDates.Add(closed.Date))
                    {
                        warnings.Add($"El dia {closed.Date:yyyy-MM-dd} ya tiene un cierre/festivo; se omite.");
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
                holidayCount, vacationDays, customClosedDays, totalWorkingDays);
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
