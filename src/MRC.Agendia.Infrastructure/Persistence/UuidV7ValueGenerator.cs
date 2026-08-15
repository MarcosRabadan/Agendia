using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ValueGeneration;

namespace MRC.Agendia.Infrastructure.Persistence
{
    /// <summary>
    /// Generates sequential UUIDv7 keys client-side, at Add time. Used for the Guid
    /// primary keys of Agendia-owned entities so ids are known before SaveChanges
    /// (avoids two just-added entities colliding on Guid.Empty) and are index-local.
    /// EF only invokes it when the key is unset, so a provisioned projection that
    /// supplies an external Guid keeps the supplied value.
    /// </summary>
    public sealed class UuidV7ValueGenerator : ValueGenerator<Guid>
    {
        public override bool GeneratesTemporaryValues => false;

        public override Guid Next(EntityEntry entry) => Guid.CreateVersion7();
    }
}
