namespace MRC.Agendia.Application.Statistics.DTO
{
    /// <summary>Bookings in a labelled period (e.g. "2026-05" for a month, "2026-W22" for an ISO week).</summary>
    public record PeriodCountDto(string Period, int Count);
}
