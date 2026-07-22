using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Business.DTO
{
    public record BusinessDto(
        int Id,
        string Name,
        string? Description,
        string Address,
        string Phone,
        string Email,
        bool IsActive,
        // Harmony user id of the owner. Safe to expose here: BusinessDto is only
        // returned to authenticated Admin/Owner callers - the anonymous reads go
        // through BusinessPublicDto, which deliberately omits it.
        string? OwnerUserId = null,
        int? CancellationWindowHours = null,
        string DefaultLanguage = SupportedLanguages.Spanish,
        AppointmentStatus DefaultAppointmentStatus = AppointmentStatus.Pending);
}
