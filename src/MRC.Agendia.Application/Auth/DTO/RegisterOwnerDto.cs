using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Application.Auth.DTO
{
    public record RegisterOwnerDto(
        string Email,
        string Password,
        string FullName,
        string Phone,
        string BusinessName,
        string BusinessAddress,
        string BusinessPhone,
        string BusinessEmail,
        string? BusinessDescription,
        string BusinessDefaultLanguage = SupportedLanguages.Spanish);
}
