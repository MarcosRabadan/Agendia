namespace MRC.Agendia.Tests.Integration
{
    /// <summary>
    /// Deterministic Guid from an int seed, so tests can keep using small readable
    /// ids after the int -> Guid migration while preserving relationships (the same
    /// seed always maps to the same Guid).
    /// </summary>
    public static class TestIds
    {
        public static Guid Of(int seed) => new(seed, 0, 0, new byte[8]);
    }
}
