using System.Net.Http.Headers;
using System.Net.Http.Json;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Domain.Constants;

namespace MRC.Agendia.Tests.Integration.Infrastructure
{
    /// <summary>
    /// Sets up a business and its owner the way Harmony would: it registers the user
    /// on its own side and then calls Agendia's provisioning endpoints with the
    /// resulting user id.
    ///
    /// This replaces the per-test <c>RegisterOwnerAsync</c> helpers that used to POST
    /// to /api/auth/register/owner. That endpoint auto-created the owner's Employee
    /// row as a side effect; here the Employee is created explicitly, because most
    /// scheduling and appointment tests need the owner to be a bookable employee.
    /// </summary>
    public static class TestProvisioning
    {
        public static async Task<ProvisionedOwner> ProvisionOwnerAsync(HttpClient client, string slug)
        {
            var unique = Guid.NewGuid().ToString("N");
            var ownerUserId = $"harmony-{slug}-{unique}";
            var adminToken = TestTokenFactory.Create($"admin-{unique}", Roles.Admin);

            var createBusiness = new CreateBusinessDto(Name: $"{slug}-{unique}",
                                                       Description: null,
                                                       Address: "Calle Test 1",
                                                       Phone: "910000000",
                                                       Email: $"info-{unique}@test.local",
                                                       OwnerUserId: ownerUserId);

            var business = await PostAsync<CreateBusinessDto, BusinessDto>(
                client, "/api/Business", createBusiness, adminToken);

            var ownerToken = TestTokenFactory.Create(ownerUserId, Roles.BusinessOwner);

            var createEmployee = new CreateEmployeeDto(BusinessId: business.Id,
                                                       FullName: $"Owner {slug}",
                                                       Email: $"{slug}-{unique}@test.local",
                                                       Phone: "600000000",
                                                       UserId: ownerUserId);

            var employee = await PostAsync<CreateEmployeeDto, EmployeeDto>(
                client, "/api/Employee", createEmployee, ownerToken);

            return new ProvisionedOwner(ownerUserId, ownerToken, business, employee.Id);
        }

        /// <summary>Posts with an explicit bearer token, leaving the shared client's headers untouched.</summary>
        public static async Task<TResponse> PostAsync<TRequest, TResponse>(HttpClient client,
                                                                           string url,
                                                                           TRequest body,
                                                                           string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<TResponse>();
            Assert.NotNull(result);
            return result!;
        }
    }

    public sealed record ProvisionedOwner(string OwnerUserId, string Token, BusinessDto Business, int EmployeeId);
}
