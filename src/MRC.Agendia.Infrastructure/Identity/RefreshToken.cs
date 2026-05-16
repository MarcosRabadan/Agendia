namespace MRC.Agendia.Infrastructure.Identity
{
    public class RefreshToken
    {
        public int Id { get; set; }
        public string Token { get; set; } = null!;
        public string UserId { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByToken { get; set; }

        public ApplicationUser User { get; set; } = null!;

        public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;
    }
}
