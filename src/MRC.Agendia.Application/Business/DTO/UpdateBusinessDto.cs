using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Business.DTO
{
    public record UpdateBusinessDto(
        int Id,
        string Name,
        string? Description,
        string Address,
        string Phone,
        string Email,
        bool IsActive,
        int? CancellationWindowHours = null,
        string DefaultLanguage = SupportedLanguages.Spanish);
}
