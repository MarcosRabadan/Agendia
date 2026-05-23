using MRC.Agendia.Domain.Services;

namespace MRC.Agendia.Application.Schedules.DTO
{
    public record CalendarDayDto(
        DateOnly Date,
        bool IsOpen,
        string? Status,
        List<EffectiveTimeSlotDto>? TimeSlots)
    {
        /// <summary>
        /// Projects a resolved <see cref="EffectiveSchedule"/> into a calendar day,
        /// keeping the open/closed status text in a single place.
        /// </summary>
        public static CalendarDayDto FromEffective(EffectiveSchedule effective)
            => new(
                effective.Date,
                effective.IsOpen,
                effective.IsOpen ? "Abierto" : effective.ClosedReason ?? "Cerrado",
                effective.IsOpen
                    ? effective.TimeSlots.Select(ts => new EffectiveTimeSlotDto(ts.StartTime, ts.EndTime)).ToList()
                    : null);
    }
}
