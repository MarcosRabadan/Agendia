namespace MRC.Agendia.Application.Clients.DTO
{
    // UserId is the Harmony user id, and is optional: a walk-in/phone client has no
    // user account. UpdateClientDto deliberately omits it so an existing client can
    // never be repointed to another user via a crafted DTO.
    public record CreateClientDto(
        string Name,
        string Phone,
        string? Email,
        string? UserId = null);
}
