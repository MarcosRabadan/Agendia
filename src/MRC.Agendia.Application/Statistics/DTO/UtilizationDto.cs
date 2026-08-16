namespace MRC.Agendia.Application.Statistics.DTO
{
    /// <summary>
    /// How well the agenda of a business is used over a date range. Everything is measured
    /// in MINUTES of agenda: offered = open minutes of the effective schedule multiplied by
    /// each employee's capacity (minus their time off), booked = minutes taken by
    /// appointments that occupied the agenda. No prices involved - the catalog service owns
    /// those, Agendia owns the calendar.
    ///
    /// <para><see cref="AvgLeadTimeHours"/> is how far in advance clients book, on average:
    /// the gap between when the appointment was created and when it starts.</para>
    /// </summary>
    public record UtilizationDto(
        DateOnly From,
        DateOnly To,
        int OfferedMinutes,
        int BookedMinutes,
        double OccupancyRate,
        double AvgLeadTimeHours,
        IReadOnlyList<HourUtilizationDto> ByHour,
        IReadOnlyList<WeekdayUtilizationDto> ByWeekday,
        IReadOnlyList<EmployeeUtilizationDto> ByEmployee);
}
