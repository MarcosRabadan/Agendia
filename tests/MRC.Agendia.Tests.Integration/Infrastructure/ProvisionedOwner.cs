using MRC.Agendia.Application.Business.DTO;

namespace MRC.Agendia.Tests.Integration.Infrastructure
{
    /// <summary>A business owner as Harmony would have provisioned it, plus their token.</summary>
    public sealed record ProvisionedOwner(string OwnerUserId, string Token, BusinessDto Business, int EmployeeId);
}
