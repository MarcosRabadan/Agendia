using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Statistics.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Statistics
{
    /// <summary>
    /// End-to-end coverage for the business statistics panel (issue #169): the
    /// owner gets aggregated metrics for a date range, and the endpoint is limited
    /// to the owner/admin of that business.
    /// </summary>
    public class BusinessStatsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string OwnerPassword = "Owner1234!";

        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public BusinessStatsIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetStats_ComoDueno_DevuelveMetricas()
        {
            var owner = await RegisterOwnerAsync("stats-ok");
            await SeedAppointmentsAsync(owner.Business.Id);

            var stats = await GetStatsAsync(owner.Token, owner.Business.Id, "2026-05-01", "2026-05-31");

            Assert.Equal(3, stats.TotalAppointments);
            Assert.Equal(2, stats.TotalBookings);   // completed + confirmed
            Assert.Equal(30m, stats.TotalRevenue);  // one completed x 30
            Assert.Equal(1, stats.NoShowCount);
            Assert.Contains(stats.Services, s => s.ServiceName == "Corte" && s.Count >= 1);
        }

        [Fact]
        public async Task GetStats_DeOtroNegocio_DevuelveForbidden()
        {
            var ownerA = await RegisterOwnerAsync("stats-a");
            var ownerB = await RegisterOwnerAsync("stats-b");

            var response = await SendStatsAsync(ownerB.Token, ownerA.Business.Id, "2026-05-01", "2026-05-31");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetStats_ComoCliente_DevuelveForbidden()
        {
            var owner = await RegisterOwnerAsync("stats-cli");
            var clientToken = await RegisterClientAndGetTokenAsync("stats-c");

            var response = await SendStatsAsync(clientToken, owner.Business.Id, "2026-05-01", "2026-05-31");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetStats_ComoEmpleado_DevuelveMetricas()
        {
            var owner = await RegisterOwnerAsync("stats-emp");
            await SeedAppointmentsAsync(owner.Business.Id);
            var employeeToken = await RegisterEmployeeAndGetTokenAsync(owner, "stats-emp");

            var stats = await GetStatsAsync(employeeToken, owner.Business.Id, "2026-05-01", "2026-05-31");

            Assert.Equal(3, stats.TotalAppointments);
            Assert.Equal(30m, stats.TotalRevenue);
        }

        // ----- Helpers -----

        private async Task SeedAppointmentsAsync(int businessId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();

            var employeeId = (await db.Employees.FirstAsync(e => e.BusinessId == businessId)).Id;
            var service = new Service { BusinessId = businessId, Name = "Corte", DurationMinutes = 30, Price = 30m };
            db.Services.Add(service);
            var client = new Client { Name = "Cliente Test", Phone = "600111222" };
            db.Clients.Add(client);
            await db.SaveChangesAsync();

            db.Appointments.AddRange(
                Appointment(client.Id, employeeId, service.Id, new DateTime(2026, 5, 4, 10, 0, 0), AppointmentStatus.Completed),
                Appointment(client.Id, employeeId, service.Id, new DateTime(2026, 5, 4, 11, 0, 0), AppointmentStatus.Confirmed),
                Appointment(client.Id, employeeId, service.Id, new DateTime(2026, 5, 6, 16, 0, 0), AppointmentStatus.NoShow));
            await db.SaveChangesAsync();
        }

        private static Appointment Appointment(int clientId, int employeeId, int serviceId, DateTime start, AppointmentStatus status)
            => new()
            {
                ClientId = clientId,
                EmployeeId = employeeId,
                ServiceId = serviceId,
                StartDate = start,
                EndDate = start.AddMinutes(30),
                Status = status,
            };

        private async Task<BusinessStatsDto> GetStatsAsync(string token, int businessId, string from, string to)
        {
            var response = await SendStatsAsync(token, businessId, from, to);
            response.EnsureSuccessStatusCode();
            var stats = await response.Content.ReadFromJsonAsync<BusinessStatsDto>();
            Assert.NotNull(stats);
            return stats!;
        }

        private async Task<HttpResponseMessage> SendStatsAsync(string token, int businessId, string from, string to)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get, $"/api/businesses/{businessId}/stats?from={from}&to={to}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await _client.SendAsync(request);
        }

        private async Task<string> RegisterClientAndGetTokenAsync(string slug)
        {
            var unique = Guid.NewGuid().ToString("N");
            var dto = new RegisterClientDto($"{slug}-{unique}@test.local", "Client1234!", $"Cliente {slug}", "600999888");
            var response = await _client.PostAsJsonAsync("/api/auth/register/client", dto);
            response.EnsureSuccessStatusCode();
            var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(auth);
            return auth!.AccessToken;
        }

        private async Task<string> RegisterEmployeeAndGetTokenAsync(RegisteredOwner owner, string slug)
        {
            var unique = Guid.NewGuid().ToString("N");
            var email = $"{slug}-emp-{unique}@test.local";
            const string password = "Employee1234!";

            // The owner creates the employee account, then the employee logs in.
            var dto = new RegisterEmployeeDto(owner.Business.Id, email, password, $"Empleado {slug}", "600222333");
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register/employee")
            {
                Content = JsonContent.Create(dto)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password });
            login.EnsureSuccessStatusCode();
            var auth = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(auth);
            return auth!.AccessToken;
        }

        private async Task<RegisteredOwner> RegisterOwnerAsync(string slug)
        {
            var unique = Guid.NewGuid().ToString("N");
            var email = $"{slug}-{unique}@test.local";
            var businessName = $"{slug}-{unique}";

            var registration = new RegisterOwnerDto(
                Email: email,
                Password: OwnerPassword,
                FullName: $"Owner {slug}",
                Phone: "600000000",
                BusinessName: businessName,
                BusinessAddress: "Calle Test 1",
                BusinessPhone: "910000000",
                BusinessEmail: $"info-{unique}@test.local",
                BusinessDescription: null);

            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register/owner", registration);
            registerResponse.EnsureSuccessStatusCode();
            var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(auth);

            var businessesResponse = await _client.GetAsync("/api/Business?page=1&pageSize=200");
            businessesResponse.EnsureSuccessStatusCode();
            var paged = await businessesResponse.Content.ReadFromJsonAsync<PagedResult<BusinessPublicDto>>();
            Assert.NotNull(paged);
            var business = paged!.Items.First(b => b.Name == businessName);

            return new RegisteredOwner(auth!.AccessToken, business);
        }

        private sealed record RegisteredOwner(string Token, BusinessPublicDto Business);
    }
}
