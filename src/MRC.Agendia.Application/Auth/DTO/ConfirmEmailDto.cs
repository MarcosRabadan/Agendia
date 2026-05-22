namespace MRC.Agendia.Application.Auth.DTO
{
    public record ConfirmEmailDto(
        string UserId,
        string Token);
}
