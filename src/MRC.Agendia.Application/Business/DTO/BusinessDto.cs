using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Business.DTO
{
    // Scheduling config of a business, returned by the provisioning POST/PUT. Holds no
    // profile data (name/address/contact) - that lives in the management service.
    public record BusinessDto(
        int Id,
        bool IsActive,
        // Harmony user id of the owner. Only returned to the authenticated provisioning
        // caller (Admin/Owner), so it is safe to expose here.
        string? OwnerUserId = null,
        int? CancellationWindowHours = null,
        string DefaultLanguage = SupportedLanguages.Spanish,
        AppointmentStatus DefaultAppointmentStatus = AppointmentStatus.Pending);
}
