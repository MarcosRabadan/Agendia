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

        public async Task<EffectiveSchedule> GetEffectiveScheduleAsync(int businessId, DateOnly date)
        {
            // An override for the date wins; only hit the template store if there is none.
            var scheduleOverride = await _overrideRepository.GetByBusinessIdAndDateAsync(businessId, date);
            var template = scheduleOverride is null
                ? await _templateRepository.GetEffectiveTemplateAsync(businessId, date)
                : null;

            return BuildEffectiveSchedule(scheduleOverride, template, date);
        }

        public async Task<IEnumerable<EffectiveSchedule>> GetEffectiveSchedulesAsync(int businessId, DateOnly from, DateOnly to)
        {
            var results = new List<EffectiveSchedule>();

            for (var date = from; date <= to; date = date.AddDays(1))
            {
                results.Add(await GetEffectiveScheduleAsync(businessId, date));
            }

            return results;
        }

        public EffectiveSchedule Resolve(
            IEnumerable<ScheduleTemplate> templates,
            IEnumerable<ScheduleOverride> overrides,
            DateOnly date)
        {
            var scheduleOverride = overrides.FirstOrDefault(o => o.Date == date);
            var template = scheduleOverride is null
                ? templates
                    .Where(t => t.EffectiveFrom <= date && t.EffectiveTo >= date)
                    .OrderByDescending(t => t.IsDefault ? 0 : 1)
                    .FirstOrDefault()
                : null;

            return BuildEffectiveSchedule(scheduleOverride, template, date);
        }

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
