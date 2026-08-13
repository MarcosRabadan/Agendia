using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Application.Business.DTO
{
    public record UpdateBusinessDto(
        int Id,
        bool IsActive,
        int? CancellationWindowHours = null,
        string DefaultLanguage = SupportedLanguages.Spanish,
        AppointmentStatus DefaultAppointmentStatus = AppointmentStatus.Pending);
}
