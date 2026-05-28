namespace MRC.Agendia.Application.Statistics.DTO
{
    /// <summary>
    /// Business statistics for a date range. Counts use "bookings" = Pending +
    /// Confirmed + Completed; revenue is the sum of the service price of Completed
    /// appointments (current price, not a historical snapshot). No-show and
    /// cancellation rates are fractions (0-1) over the total appointments in range.
    /// </summary>
    public record BusinessStatsDto(
        DateOnly From,
        DateOnly To,
        int TotalAppointments,
        int TotalBookings,
        decimal TotalRevenue,
        int NoShowCount,
        double NoShowRate,
        int CancelledCount,
        double CancellationRate,
        IReadOnlyList<PeriodCountDto> ByMonth,
        IReadOnlyList<PeriodCountDto> ByWeek,
        IReadOnlyList<ServiceUsageDto> Services,
        IReadOnlyList<HourStatsDto> ByHour,
        IReadOnlyList<DayOfWeekStatsDto> ByDayOfWeek);
}
