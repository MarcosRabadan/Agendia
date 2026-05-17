namespace MRC.Agendia.Domain.Constants
{
    /// <summary>
    /// Comma-separated role combinations for <c>[Authorize(Roles = ...)]</c>.
    /// Keeps the controllers free of repeated string concatenations such as
    /// <c>Roles.Admin + "," + Roles.BusinessOwner</c> and gives each combo a
    /// readable name that matches the intent (who is allowed to do this?).
    ///
    /// If a combo needs to grow (e.g. adding a "Manager" role), it changes here
    /// and every endpoint using the constant follows automatically.
    /// </summary>
    public static class RolePolicies
    {
        /// <summary>Admin OR the BusinessOwner who owns the resource.</summary>
        public const string AdminOrOwner = Roles.Admin + "," + Roles.BusinessOwner;

        /// <summary>Admin OR any staff member of the business (owner or employee).</summary>
        public const string Staff = Roles.Admin + "," + Roles.BusinessOwner + "," + Roles.Employee;

        /// <summary>Admin OR the Client who owns the resource.</summary>
        public const string AdminOrSelfClient = Roles.Admin + "," + Roles.Client;
    }
}
