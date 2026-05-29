using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Business.DTO
{
    public record CreateBusinessDto(
        string Name,
        string? Description,
        string Address,
        string Phone,
        string Email,
        int? CancellationWindowHours = null,
        string DefaultLanguage = SupportedLanguages.Spanish);
}
