using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Application.Statistics.DTO;
using MRC.Agendia.Domain.Constants;
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
            await SeedAppointmentsAsync(owner);

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
            var clientToken = (await TestProvisioning.ProvisionClientAsync(_client, "stats-c")).Token;

            var response = await SendStatsAsync(clientToken, owner.Business.Id, "2026-05-01", "2026-05-31");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetStats_ComoEmpleado_DevuelveMetricas()
        {
            var owner = await RegisterOwnerAsync("stats-emp");
            await SeedAppointmentsAsync(owner);
            var employeeToken = await CreateEmployeeAccountAsync(owner, "stats-emp");

            var stats = await GetStatsAsync(employeeToken, owner.Business.Id, "2026-05-01", "2026-05-31");

            Assert.Equal(3, stats.TotalAppointments);
            Assert.Equal(30m, stats.TotalRevenue);
        }

        // ----- Helpers -----

        private async Task SeedAppointmentsAsync(ProvisionedOwner owner)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();

            var service = new Service { BusinessId = owner.Business.Id, Name = "Corte", DurationMinutes = 30, Price = 30m };
            db.Services.Add(service);
            var client = new Client { Name = "Cliente Test", Phone = "600111222" };
            db.Clients.Add(client);
            await db.SaveChangesAsync();

            db.Appointments.AddRange(
                Appointment(client.Id, owner.EmployeeId, service.Id, new DateTime(2026, 5, 4, 10, 0, 0), AppointmentStatus.Completed),
                Appointment(client.Id, owner.EmployeeId, service.Id, new DateTime(2026, 5, 4, 11, 0, 0), AppointmentStatus.Confirmed),
                Appointment(client.Id, owner.EmployeeId, service.Id, new DateTime(2026, 5, 6, 16, 0, 0), AppointmentStatus.NoShow));
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

        /// <summary>
        /// The owner creates an employee bound to a Harmony user id; the token is then
        /// minted for that same user id so the employee is recognised as staff of the
        /// business (this replaces the old register-employee + login round-trip).
        /// </summary>
        private async Task<string> CreateEmployeeAccountAsync(ProvisionedOwner owner, string slug)
        {
            var unique = Guid.NewGuid().ToString("N");
            var employeeUserId = $"harmony-employee-{unique}";

            await TestProvisioning.PostAsync<CreateEmployeeDto, EmployeeDto>(
                _client,
                "/api/Employee",
                new CreateEmployeeDto(BusinessId: owner.Business.Id,
                                      FullName: $"Empleado {slug}",
                                      Email: $"{slug}-emp-{unique}@test.local",
                                      Phone: "600222333",
                                      UserId: employeeUserId),
                owner.Token);

            return TestTokenFactory.Create(employeeUserId, Roles.Employee);
        }

        private Task<ProvisionedOwner> RegisterOwnerAsync(string slug) =>
            TestProvisioning.ProvisionOwnerAsync(_client, slug);
    }
}
