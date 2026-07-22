using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Business.DTO
{
    // OwnerUserId is the Harmony user id of the owner. It is required: a business
    // with no owner cannot be managed by anyone but an Admin, and there is no way
    // to assign one afterwards (UpdateBusinessDto deliberately omits it, so the
    // owner of an existing business can never be repointed via a crafted DTO).
    public record CreateBusinessDto(
        string Name,
        string? Description,
        string Address,
        string Phone,
        string Email,
        string OwnerUserId,
        int? CancellationWindowHours = null,
        string DefaultLanguage = SupportedLanguages.Spanish,
        AppointmentStatus DefaultAppointmentStatus = AppointmentStatus.Pending);
}
