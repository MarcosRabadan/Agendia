using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Api.Filters;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Appointments
{
    /// <summary>
    /// End-to-end coverage for the opt-in <c>Idempotency-Key</c> on booking (#266): a retry
    /// of the same request replays the original appointment instead of creating a second
    /// one, the key cannot be recycled for a different payload, a rejected attempt frees
    /// the key, and a request without the header behaves exactly as before.
    /// </summary>
    public class IdempotentBookingIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const int Year = 2035;
        private static readonly DateOnly Day = new(Year, 6, 4);

        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public IdempotentBookingIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Retrying_with_the_same_key_replays_the_first_appointment()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "idem-replay", Year);
            var key = Guid.NewGuid().ToString();
            var booking = Booking(setup, new TimeOnly(9, 0));

            var first = await PostAsync(setup.OwnerToken, booking, key);
            var second = await PostAsync(setup.OwnerToken, booking, key);

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            // The retry gets the original answer back, not the 400 a duplicate booking
            // would earn (the employee's only slot is already taken by the first one).
            Assert.Equal(HttpStatusCode.Created, second.StatusCode);

            var created = await first.Content.ReadFromJsonAsync<AppointmentDto>();
            var replayed = await second.Content.ReadFromJsonAsync<AppointmentDto>();
            Assert.Equal(created!.Id, replayed!.Id);

            Assert.Equal(1, await CountAppointmentsAsync(setup.EmployeeId));
        }

        [Fact]
        public async Task Reusing_the_key_for_a_different_body_is_rejected()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "idem-reuse", Year);
            var key = Guid.NewGuid().ToString();

            (await PostAsync(setup.OwnerToken, Booking(setup, new TimeOnly(9, 0)), key)).EnsureSuccessStatusCode();

            var response = await PostAsync(setup.OwnerToken, Booking(setup, new TimeOnly(11, 0)), key);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("IDEMPOTENCY_KEY_REUSED", error!.Code);
            Assert.Equal(1, await CountAppointmentsAsync(setup.EmployeeId));
        }

        [Fact]
        public async Task A_rejected_attempt_frees_the_key_for_a_corrected_retry()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "idem-release", Year);
            var key = Guid.NewGuid().ToString();

            // 20:00 is outside the 09:00-18:00 schedule: the booking is rejected.
            var rejected = await PostAsync(setup.OwnerToken, Booking(setup, new TimeOnly(20, 0)), key);
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);

            // Same key, corrected request: the failed attempt did not burn it.
            var retried = await PostAsync(setup.OwnerToken, Booking(setup, new TimeOnly(9, 0)), key);

            Assert.Equal(HttpStatusCode.Created, retried.StatusCode);
        }

        [Fact]
        public async Task Without_the_header_nothing_changes()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "idem-optin", Year);

            var first = await PostAsync(setup.OwnerToken, Booking(setup, new TimeOnly(9, 0)), key: null);
            var second = await PostAsync(setup.OwnerToken, Booking(setup, new TimeOnly(10, 0)), key: null);

            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(HttpStatusCode.Created, second.StatusCode);
            Assert.Equal(2, await CountAppointmentsAsync(setup.EmployeeId));
        }

        [Fact]
        public async Task The_same_key_from_another_caller_is_not_shared()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "idem-scope", Year);
            var otherOwner = await TestProvisioning.ProvisionOwnerAsync(_client, "idem-scope-other");
            var key = Guid.NewGuid().ToString();

            var first = await PostAsync(setup.OwnerToken, Booking(setup, new TimeOnly(9, 0)), key);
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);

            // Another caller reusing the same header value must never read the first
            // caller's response: their request runs on its own merits and is rejected for
            // booking on someone else's employee (404, the cross-tenant convention: a
            // stranger is not told the resource exists).
            var second = await PostAsync(otherOwner.Token, Booking(setup, new TimeOnly(12, 0)), key);

            Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
        }

        // ----- Helpers -----

        private static CreateAppointmentDto Booking(BookableBusiness setup, TimeOnly at)
        {
            var start = Day.ToDateTime(at);
            return new CreateAppointmentDto(setup.ClientUserId, setup.EmployeeId, setup.Service.Id,
                start, start.AddMinutes(30), null);
        }

        private async Task<HttpResponseMessage> PostAsync(string token, CreateAppointmentDto dto, string? key)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Appointment")
            {
                Content = JsonContent.Create(dto)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (key is not null)
                request.Headers.Add(IdempotencyFilter.HeaderName, key);

            return await _client.SendAsync(request);
        }

        private async Task<int> CountAppointmentsAsync(Guid employeeId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            return await db.Appointments.CountAsync(a => a.EmployeeId == employeeId);
        }
    }
}
