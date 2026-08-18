namespace MRC.Agendia.Application.Availability
{
    /// <summary>
    /// Seats held by OTHER clients over a candidate window, split by the level they are
    /// accounted at: a hold naming an employee takes a seat from THAT employee, while an
    /// "any employee" hold takes one from the business total. The two levels never overlap,
    /// so no seat is discounted twice.
    /// </summary>
    /// <param name="ByEmployee">Seats held per employee, for the holds naming one.</param>
    /// <param name="AnyEmployee">Seats held on the business as a whole.</param>
    public record HeldSeats(
        IReadOnlyDictionary<Guid, int> ByEmployee,
        int AnyEmployee)
    {
        /// <summary>Nothing held: what a window with no active hold gets.</summary>
        public static readonly HeldSeats None = new(new Dictionary<Guid, int>(), 0);

        /// <summary>Seats held on that employee by other clients; 0 when none.</summary>
        public int For(Guid employeeId) => ByEmployee.TryGetValue(employeeId, out var seats) ? seats : 0;
    }
}
