namespace MRC.Agendia.Application.Statistics.DTO
{
    /// <summary>
    /// Business statistics for a date range. Counts use "bookings" = Pending +
    /// Confirmed + Completed. No-show and cancellation rates are fractions (0-1) over
    /// the total appointments in range. Revenue metrics were removed with the service
    /// price: Agendia no longer owns the price (it lives in the catalog service), so a
    /// financial panel, if needed, is composed by whoever owns the price.
    /// </summary>
    public record BusinessStatsDto(
        DateOnly From,
        DateOnly To,
        int TotalAppointments,
        int TotalBookings,
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
