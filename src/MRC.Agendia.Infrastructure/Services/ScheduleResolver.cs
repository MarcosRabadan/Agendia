using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Interfaces;
using MRC.Agendia.Domain.Services;

namespace MRC.Agendia.Infrastructure.Services
{
    public class ScheduleResolver : IScheduleResolver
    {
        private readonly IScheduleTemplateRepository _templateRepository;
        private readonly IScheduleOverrideRepository _overrideRepository;

        public ScheduleResolver(
            IScheduleTemplateRepository templateRepository,
            IScheduleOverrideRepository overrideRepository)
        {
            _templateRepository = templateRepository;
            _overrideRepository = overrideRepository;
        }

        /// <inheritdoc />
        public async Task<EffectiveSchedule> GetEffectiveScheduleAsync(int businessId, DateOnly date, CancellationToken cancellationToken = default)
        {
            // An override for the date wins; only hit the template store if there is none.
            var scheduleOverride = await _overrideRepository.GetByBusinessIdAndDateAsync(businessId, date, cancellationToken);
            var template = scheduleOverride is null
                ? await _templateRepository.GetEffectiveTemplateAsync(businessId, date, cancellationToken)
                : null;

            return BuildEffectiveSchedule(scheduleOverride, template, date);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<EffectiveSchedule>> GetEffectiveSchedulesAsync(int businessId,
                                                                                     DateOnly from,
                                                                                     DateOnly to,
                                                                                     CancellationToken cancellationToken = default)
        {
            // Load the whole range once and resolve in memory instead of issuing
            // 1-2 queries per day. The old per-day loop was an N+1 that made the
            // calendar endpoint a DoS vector for large ranges.
            var templates = await _templateRepository.GetByBusinessIdAsync(businessId, cancellationToken);
            var overrides = await _overrideRepository.GetByBusinessIdAndDateRangeAsync(businessId, from, to, cancellationToken);

            return ResolveRange(templates, overrides, from, to);
        }

        /// <inheritdoc />
        public EffectiveSchedule Resolve(
            IEnumerable<ScheduleTemplate> templates,
            IEnumerable<ScheduleOverride> overrides,
            DateOnly date)
        {
            var scheduleOverride = overrides.FirstOrDefault(o => o.Date == date);
            var template = scheduleOverride is null ? SelectTemplate(templates, date) : null;
            return BuildEffectiveSchedule(scheduleOverride, template, date);
        }

        /// <inheritdoc />
        public IEnumerable<EffectiveSchedule> ResolveRange(
            IEnumerable<ScheduleTemplate> templates,
            IEnumerable<ScheduleOverride> overrides,
            DateOnly from,
            DateOnly to)
        {
            var templateList = templates as IReadOnlyCollection<ScheduleTemplate> ?? templates.ToList();

            // (BusinessId, Date) is unique per business, but a preview merges
            // existing + generated overrides which can collide on a date; keep the
            // first occurrence to match the previous FirstOrDefault behaviour.
            var overridesByDate = new Dictionary<DateOnly, ScheduleOverride>();
            foreach (var scheduleOverride in overrides)
                overridesByDate.TryAdd(scheduleOverride.Date, scheduleOverride);

            var results = new List<EffectiveSchedule>();
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                overridesByDate.TryGetValue(date, out var todayOverride);
                var template = todayOverride is null ? SelectTemplate(templateList, date) : null;
                results.Add(BuildEffectiveSchedule(todayOverride, template, date));
            }

            return results;
        }

        private static ScheduleTemplate? SelectTemplate(IEnumerable<ScheduleTemplate> templates, DateOnly date)
            => templates
                .Where(t => t.EffectiveFrom <= date && t.EffectiveTo >= date)
                .OrderByDescending(t => t.IsDefault)
                .FirstOrDefault();

        /// <summary>
        /// Single source of truth for turning an (optional) override + (optional)
        /// template into an EffectiveSchedule for a given date. Shared by the
        /// DB-backed lookups and the in-memory <see cref="Resolve"/>.
        /// </summary>
        private static EffectiveSchedule BuildEffectiveSchedule(
            ScheduleOverride? scheduleOverride,
            ScheduleTemplate? template,
            DateOnly date)
        {
            if (scheduleOverride != null)
            {
                if (scheduleOverride.OverrideType == ScheduleOverrideType.CustomHours)
                {
                    return new EffectiveSchedule
                    {
                        Date = date,
                        IsOpen = true,
                        OverrideType = scheduleOverride.OverrideType,
                        ClosedReason = null,
                        TimeSlots = scheduleOverride.CustomSlots
                            .Select(cs => new EffectiveTimeSlot
                            {
                                StartTime = cs.StartTime,
                                EndTime = cs.EndTime
                            })
                            .OrderBy(ts => ts.StartTime)
                            .ToList()
                    };
                }

                // Closed, NationalHoliday, LocalHoliday
                return new EffectiveSchedule
                {
                    Date = date,
                    IsOpen = false,
                    OverrideType = scheduleOverride.OverrideType,
                    ClosedReason = scheduleOverride.Reason ?? scheduleOverride.OverrideType.ToString(),
                    TimeSlots = new List<EffectiveTimeSlot>()
                };
            }

            if (template == null)
            {
                return new EffectiveSchedule
                {
                    Date = date,
                    IsOpen = false,
                    ClosedReason = "Sin horario definido",
                    TimeSlots = new List<EffectiveTimeSlot>()
                };
            }

            var daySlots = template.WeeklySlots
                .Where(ws => ws.DayOfWeek == date.DayOfWeek)
                .OrderBy(ws => ws.StartTime)
                .Select(ws => new EffectiveTimeSlot
                {
                    StartTime = ws.StartTime,
                    EndTime = ws.EndTime
                })
                .ToList();

            return new EffectiveSchedule
            {
                Date = date,
                IsOpen = daySlots.Count > 0,
                ClosedReason = daySlots.Count > 0 ? null : "Dia no laborable",
                TimeSlots = daySlots
            };
        }
    }
}
