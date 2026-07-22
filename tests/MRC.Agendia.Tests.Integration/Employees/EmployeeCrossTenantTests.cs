using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Employees
{
    /// <summary>
    /// Integration tests for issue #87. Cover the cross-tenant scenarios on the
    /// Employee endpoints:
    ///   - Owner A cannot create an employee inside business B.
    ///   - Owner A cannot see employees of business B in GET /api/Employee.
    /// Owner A operating on his own business remains allowed.
    /// </summary>
    public class EmployeeCrossTenantTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public EmployeeCrossTenantTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task CreateEmployee_OwnerA_EnBusinessB_DevuelveForbidden()
        {
            var ownerA = await RegisterOwnerAsync("pelu-a");
            var ownerB = await RegisterOwnerAsync("pelu-b");

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Employee")
            {
                Content = JsonContent.Create(new CreateEmployeeDto(
                    BusinessId: ownerB.Business.Id,
                    FullName: "Hacker Stylist",
                    Email: null,
                    Phone: "600000999",
                    MaxConcurrentAppointments: 1))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerA.Token);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains("No tienes permiso", body);
        }

        [Fact]
        public async Task CreateEmployee_OwnerA_EnSuPropioBusiness_DevuelveCreated()
        {
            var ownerA = await RegisterOwnerAsync("pelu-a");

            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Employee")
            {
                Content = JsonContent.Create(new CreateEmployeeDto(
                    BusinessId: ownerA.Business.Id,
                    FullName: "New Stylist",
                    Email: null,
                    Phone: "600000111",
                    MaxConcurrentAppointments: 1))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerA.Token);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<EmployeeDto>();
            Assert.NotNull(created);
            Assert.Equal(ownerA.Business.Id, created!.BusinessId);
        }

        [Fact]
        public async Task GetAllEmployees_OwnerA_SoloVeLosDeSuBusiness()
        {
            var ownerA = await RegisterOwnerAsync("pelu-a");
            var ownerB = await RegisterOwnerAsync("pelu-b");

            // Provisioning creates each owner's own Employee row, so both tenants have
            // at least one employee. Owner A must see his own, never the one of B.
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Employee?page=1&pageSize=200");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerA.Token);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var paged = await response.Content.ReadFromJsonAsync<PagedResult<EmployeeDto>>();
            Assert.NotNull(paged);
            Assert.NotEmpty(paged!.Items);
            Assert.All(paged.Items, e => Assert.Equal(ownerA.Business.Id, e.BusinessId));
            Assert.DoesNotContain(paged.Items, e => e.BusinessId == ownerB.Business.Id);
        }

        [Fact]
        public async Task UpdateEmployee_ConBusinessIdAjenoEnElBody_NoMueveDeTenant()
        {
            var ownerA = await RegisterOwnerAsync("emp-stay-a");
            var ownerB = await RegisterOwnerAsync("emp-stay-b");

            var employee = await CreateEmployeeAsAsync(ownerA, "Stylist A");

            // Crafted PUT: a raw "businessId" field pointing at business B. The DTO
            // no longer exposes BusinessId and the mapping ignores it, so the
            // employee must stay in business A (issue #125).
            var crafted = new
            {
                id = employee.Id,
                businessId = ownerB.Business.Id,
                fullName = "Stylist A",
                email = (string?)null,
                phone = "600000111",
                isActive = true,
                maxConcurrentAppointments = 1
            };

            using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/Employee/{employee.Id}")
            {
                Content = JsonContent.Create(crafted)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerA.Token);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var updated = await response.Content.ReadFromJsonAsync<EmployeeDto>();
            Assert.NotNull(updated);
            Assert.Equal(ownerA.Business.Id, updated!.BusinessId);
        }

        // ----- Helpers -----

        private async Task<EmployeeDto> CreateEmployeeAsAsync(ProvisionedOwner owner, string fullName)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Employee")
            {
                Content = JsonContent.Create(new CreateEmployeeDto(
                    BusinessId: owner.Business.Id,
                    FullName: fullName,
                    Email: null,
                    Phone: "600000111",
                    MaxConcurrentAppointments: 1))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<EmployeeDto>();
            Assert.NotNull(created);
            return created!;
        }

        // Every call provisions a brand new owner user id, so owner A and owner B
        // are distinct identities holding distinct tokens.
        private Task<ProvisionedOwner> RegisterOwnerAsync(string slug) =>
            TestProvisioning.ProvisionOwnerAsync(_client, slug);
    }
}
