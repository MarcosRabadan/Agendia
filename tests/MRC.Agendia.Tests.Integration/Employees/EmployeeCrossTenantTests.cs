using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Application.Business.DTO;
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
        private const string OwnerPassword = "Owner1234!";
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

            // Each owner registration auto-creates the owner's Employee (logic from #71).
            // Owner A must see his own employee, never the one of B.
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

        private async Task<EmployeeDto> CreateEmployeeAsAsync(RegisteredOwner owner, string fullName)
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

        private async Task<RegisteredOwner> RegisterOwnerAsync(string slug)
        {
            var uniqueSuffix = Guid.NewGuid().ToString("N");
            var email = $"{slug}-{uniqueSuffix}@test.local";
            var businessName = $"{slug}-{uniqueSuffix}";

            var registration = new RegisterOwnerDto(
                Email: email,
                Password: OwnerPassword,
                FullName: $"Owner {slug}",
                Phone: "600000000",
                BusinessName: businessName,
                BusinessAddress: "Calle Test 1",
                BusinessPhone: "910000000",
                BusinessEmail: $"info-{uniqueSuffix}@test.local",
                BusinessDescription: null);

            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register/owner", registration);
            registerResponse.EnsureSuccessStatusCode();
            var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(auth);

            // GET /api/Business is AllowAnonymous, so we can fetch the new business by
            // its unique name without dragging the test through more auth flows.
            var businessesResponse = await _client.GetAsync("/api/Business?page=1&pageSize=200");
            businessesResponse.EnsureSuccessStatusCode();
            var paged = await businessesResponse.Content.ReadFromJsonAsync<PagedResult<BusinessDto>>();
            Assert.NotNull(paged);
            var business = paged!.Items.First(b => b.Name == businessName);

            return new RegisteredOwner(auth!.AccessToken, business);
        }

        private sealed record RegisteredOwner(string Token, BusinessDto Business);
    }
}
