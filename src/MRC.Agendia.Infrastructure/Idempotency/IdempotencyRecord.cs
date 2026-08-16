namespace MRC.Agendia.Infrastructure.Idempotency
{
    /// <summary>
    /// One request served (or being served) under an <c>Idempotency-Key</c>. This is a
    /// persistence detail of the API surface, not a domain entity: no soft delete and no
    /// audit fields, and rows are purged once they age past the retention window.
    /// </summary>
    public class IdempotencyRecord
    {
        /// <summary>Storage key: the caller's user id plus the header value.</summary>
        public string Key { get; set; } = null!;

        /// <summary>Hash of the endpoint plus the request payload, to detect key reuse.</summary>
        public string RequestHash { get; set; } = null!;

        /// <summary>Status code of the stored response, or null while the request is in flight.</summary>
        public int? StatusCode { get; set; }

        /// <summary>Serialized response body, or null while the request is in flight.</summary>
        public string? ResponseBody { get; set; }

        /// <summary>UTC instant the key was claimed. Drives the retention purge.</summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>UTC instant the response was stored, or null while in flight.</summary>
        public DateTime? CompletedAtUtc { get; set; }
    }
}
