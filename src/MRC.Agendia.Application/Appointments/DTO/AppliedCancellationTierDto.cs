using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Appointments.DTO
{
    /// <summary>
    /// The cancellation tier that applied to a self-service cancellation: how much notice
    /// it required and what it costs the client. Agendia does NOT charge anything - it
    /// reports the rule so the front (and the payments service) can act on it.
    /// </summary>
    public record AppliedCancellationTierDto(
        int MinHoursBefore,
        CancellationPenaltyKind PenaltyKind,
        decimal? PenaltyValue);
}
