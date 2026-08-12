using System.Net;
using System.Net.Http.Json;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Auditing.DTO;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Auth
{
    /// <summary>
    /// End-to-end coverage of the admin audit-log endpoint: it is Admin-gated
    /// (401 without a token, 403 for a non-admin), and a state-changing action —
    /// an appointment status change — is recorded and retrievable by its action code.
    /// </summary>
    public class AuditLogEndpointIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const int Year = 2035;
        private static readonly DateOnly Day = new(Year, 6, 4);

        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AuditLogEndpointIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Endpoint_requires_a_token()
        {
            var response = await _client.GetAsync("/api/admin/audit-logs");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Non_admin_is_forbidden()
        {
            var token = TestTokenFactory.Create("owner-x", Roles.BusinessOwner);
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Get, "/api/admin/audit-logs", token);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Admin_can_read_the_audit_log()
        {
            var token = TestTokenFactory.Create("admin-read", Roles.Admin);
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Get, "/api/admin/audit-logs?page=1&pageSize=10", token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var page = await response.Content.ReadFromJsonAsync<PagedResult<AuditLogDto>>();
            Assert.NotNull(page);
        }

        [Fact]
        public async Task Appointment_status_change_is_audited_and_retrievable()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "audit-status", Year);

            var start = Day.ToDateTime(new TimeOnly(9, 0));
            var created = await (await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                new CreateAppointmentDto(setup.ClientId, setup.EmployeeId, setup.Service.Id, start, start.AddMinutes(30), null)))
                .Content.ReadFromJsonAsync<AppointmentDto>();

            var update = new UpdateAppointmentDto(created!.Id, created.ClientId, created.EmployeeId, created.ServiceId,
                created.StartDate, created.EndDate, AppointmentStatus.Confirmed, created.Notes);
            (await BookableBusinessFactory.SendAsync(_client, HttpMethod.Put, $"/api/Appointment/{created.Id}", setup.OwnerToken, update))
                .EnsureSuccessStatusCode();

            var adminToken = TestTokenFactory.Create("admin-audit", Roles.Admin);
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Get,
                $"/api/admin/audit-logs?action={AuditActions.AppointmentStatusChanged}&page=1&pageSize=50", adminToken);
            response.EnsureSuccessStatusCode();

            var page = await response.Content.ReadFromJsonAsync<PagedResult<AuditLogDto>>();
            Assert.NotNull(page);
            Assert.Contains(page!.Items, e => e.Action == AuditActions.AppointmentStatusChanged);
        }
    }
}
