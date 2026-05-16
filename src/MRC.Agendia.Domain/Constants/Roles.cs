namespace MRC.Agendia.Domain.Constants
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string BusinessOwner = "BusinessOwner";
        public const string Employee = "Employee";
        public const string Client = "Client";

        public static readonly string[] All = { Admin, BusinessOwner, Employee, Client };
    }
}
