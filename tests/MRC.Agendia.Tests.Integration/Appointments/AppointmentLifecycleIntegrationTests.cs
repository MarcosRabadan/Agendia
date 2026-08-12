using System.Net;
using System.Net.Http.Json;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Appointments
{
    /// <summary>
    /// End-to-end lifecycle of an appointment through the API: the initial status is
    /// the business default, staff can advance it to a terminal state, a terminal
    /// appointment cannot transition again, a client may only cancel their own, and
    /// scheduling rules (conflict / outside schedule) reject bad bookings with the
    /// right error codes.
    /// </summary>
    public class AppointmentLifecycleIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const int Year = 2035;
        private static readonly DateOnly Day = new(Year, 6, 4);

        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AppointmentLifecycleIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private static CreateAppointmentDto Booking(BookableBusiness s, int clientId, TimeOnly at, int minutes = 30)
        {
            var start = Day.ToDateTime(at);
            return new CreateAppointmentDto(clientId, s.EmployeeId, s.Service.Id, start, start.AddMinutes(minutes), null);
        }

        private Task<HttpResponseMessage> PutStatusAsync(string token, AppointmentDto appt, AppointmentStatus status)
            => BookableBusinessFactory.SendAsync(_client, HttpMethod.Put, $"/api/Appointment/{appt.Id}", token,
                new UpdateAppointmentDto(appt.Id, appt.ClientId, appt.EmployeeId, appt.ServiceId,
                    appt.StartDate, appt.EndDate, status, appt.Notes));

        [Fact]
        public async Task Created_appointment_starts_with_the_business_default_status()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "life-default", Year);

            var response = await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                Booking(setup, setup.ClientId, new TimeOnly(9, 0)));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<AppointmentDto>();
            Assert.Equal(AppointmentStatus.Pending, created!.Status);
        }

        [Fact]
        public async Task Staff_can_advance_status_to_completed()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "life-advance", Year);
            var created = await (await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                Booking(setup, setup.ClientId, new TimeOnly(9, 0)))).Content.ReadFromJsonAsync<AppointmentDto>();

            var confirmed = await PutStatusAsync(setup.OwnerToken, created!, AppointmentStatus.Confirmed);
            Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);

            var completedResp = await PutStatusAsync(setup.OwnerToken,
                (await confirmed.Content.ReadFromJsonAsync<AppointmentDto>())!, AppointmentStatus.Completed);
            Assert.Equal(HttpStatusCode.OK, completedResp.StatusCode);
            var completed = await completedResp.Content.ReadFromJsonAsync<AppointmentDto>();
            Assert.Equal(AppointmentStatus.Completed, completed!.Status);
        }

        [Fact]
        public async Task Terminal_appointment_cannot_transition_again()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "life-terminal", Year);
            var created = await (await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                Booking(setup, setup.ClientId, new TimeOnly(9, 0)))).Content.ReadFromJsonAsync<AppointmentDto>();
            var completed = await (await PutStatusAsync(setup.OwnerToken, created!, AppointmentStatus.Completed))
                .Content.ReadFromJsonAsync<AppointmentDto>();

            var response = await PutStatusAsync(setup.OwnerToken, completed!, AppointmentStatus.Confirmed);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("INVALID_APPOINTMENT_STATUS_TRANSITION", error!.Code);
        }

        [Fact]
        public async Task Double_booking_the_last_slot_conflicts()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "life-conflict", Year);
            var second = BookableBusinessFactory.SeedCounterClient(_factory.Services);

            (await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                Booking(setup, setup.ClientId, new TimeOnly(10, 0)))).EnsureSuccessStatusCode();

            var response = await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                Booking(setup, second, new TimeOnly(10, 0)));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("APPOINTMENT_CONFLICT", error!.Code);
        }

        [Fact]
        public async Task Booking_outside_the_schedule_is_rejected()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "life-outside", Year);

            // Schedule is 09:00–18:00; 20:00 is closed.
            var response = await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                Booking(setup, setup.ClientId, new TimeOnly(20, 0)));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("APPOINTMENT_OUTSIDE_SCHEDULE", error!.Code);
        }

        [Fact]
        public async Task Client_may_cancel_own_but_not_confirm()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "life-client", Year);
            var clientAccount = await TestProvisioning.ProvisionClientAsync(_client, "life");

            // The client books their own appointment.
            var created = await (await BookableBusinessFactory.PostAppointmentAsync(_client, clientAccount.Token,
                Booking(setup, clientAccount.ClientId, new TimeOnly(12, 0)))).Content.ReadFromJsonAsync<AppointmentDto>();

            // Confirm is staff-only -> 403 for a client.
            var confirm = await PutStatusAsync(clientAccount.Token, created!, AppointmentStatus.Confirmed);
            Assert.Equal(HttpStatusCode.Forbidden, confirm.StatusCode);

            // Cancel is allowed for the owning client.
            var cancel = await PutStatusAsync(clientAccount.Token, created!, AppointmentStatus.Cancelled);
            Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        }
    }
}
