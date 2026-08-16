using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Appointments
{
    /// <summary>
    /// End-to-end booking through the API against a REAL PostgreSQL: the same path
    /// production takes (controller, handler, scheduling validator, advisory-lock
    /// guard, transactional outbox), so it covers what the InMemory suite structurally
    /// cannot - wall-clock timestamp columns, a real transaction, and the advisory lock
    /// that serializes concurrent bookings.
    ///
    /// Skipped automatically when Docker is unavailable.
    /// </summary>
    [Collection(PostgresApiCollection.Name)]
    public class AppointmentApiPostgresTests
    {
        private const int Year = 2035;
        private static readonly DateOnly Day = new(Year, 6, 4);

        private readonly PostgresWebApplicationFactory _factory;

        public AppointmentApiPostgresTests(PostgresWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static CreateAppointmentDto Booking(BookableBusiness setup, string clientUserId, TimeOnly at)
        {
            var start = Day.ToDateTime(at);
            return new CreateAppointmentDto(clientUserId, setup.EmployeeId, setup.Service.Id,
                start, start.AddMinutes(30), null);
        }

        [SkippableFact]
        public async Task Booking_over_http_persists_wall_clock_dates_and_writes_the_outbox()
        {
            Skip.IfNot(_factory.Available, "Docker/Postgres not available; test skipped.");
            await _factory.ResetDatabaseAsync();

            var client = _factory.CreateClient();
            var setup = await BookableBusinessFactory.CreateAsync(client, _factory.Services, "pg-create", Year);
            var start = Day.ToDateTime(new TimeOnly(9, 0));

            var response = await BookableBusinessFactory.PostAppointmentAsync(client, setup.OwnerToken,
                Booking(setup, setup.ClientUserId, new TimeOnly(9, 0)));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var created = await response.Content.ReadFromJsonAsync<AppointmentDto>();

            // Read back straight from Postgres: the wall-clock instant must survive the
            // round-trip with no timezone shift and no forced UTC Kind (this is the whole
            // point of mapping StartDate/EndDate to `timestamp without time zone`).
            await using var db = _factory.CreateDbContext();
            var persisted = await db.Appointments.SingleAsync(a => a.Id == created!.Id);
            Assert.Equal(start, persisted.StartDate);
            Assert.Equal(start.AddMinutes(30), persisted.EndDate);
            Assert.Equal(DateTimeKind.Unspecified, persisted.StartDate.Kind);

            // The confirmation event committed with the appointment (transactional outbox).
            // The dispatcher is disabled in this host, so the row is still pending.
            var outbox = await db.OutboxMessages.Where(m => m.Type == "AppointmentConfirmed").ToListAsync();
            var message = Assert.Single(outbox);
            Assert.Null(message.ProcessedOnUtc);
            Assert.Contains(created!.Id.ToString(), message.Payload);
        }

        [SkippableFact]
        public async Task Double_booking_the_last_slot_is_rejected_with_a_conflict()
        {
            Skip.IfNot(_factory.Available, "Docker/Postgres not available; test skipped.");
            await _factory.ResetDatabaseAsync();

            var client = _factory.CreateClient();
            var setup = await BookableBusinessFactory.CreateAsync(client, _factory.Services, "pg-conflict", Year);

            (await BookableBusinessFactory.PostAppointmentAsync(client, setup.OwnerToken,
                Booking(setup, setup.ClientUserId, new TimeOnly(10, 0)))).EnsureSuccessStatusCode();

            var response = await BookableBusinessFactory.PostAppointmentAsync(client, setup.OwnerToken,
                Booking(setup, BookableBusinessFactory.CounterClientUserId(), new TimeOnly(10, 0)));

            // A domain rejection (400), not the 500 an unhandled database error would give.
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("APPOINTMENT_CONFLICT", error!.Code);

            await using var db = _factory.CreateDbContext();
            Assert.Equal(1, await db.Appointments.CountAsync());
        }

        [SkippableFact]
        public async Task Concurrent_bookings_of_the_same_slot_only_let_one_through()
        {
            Skip.IfNot(_factory.Available, "Docker/Postgres not available; test skipped.");
            await _factory.ResetDatabaseAsync();

            var client = _factory.CreateClient();
            var setup = await BookableBusinessFactory.CreateAsync(client, _factory.Services, "pg-race", Year);

            // Both requests hit the same employee/day, so pg_advisory_xact_lock serializes
            // them: the loser sees the winner's appointment and fails the capacity check.
            // On InMemory the guard is a no-op, so this race is only observable here.
            var first = BookableBusinessFactory.PostAppointmentAsync(client, setup.OwnerToken,
                Booking(setup, BookableBusinessFactory.CounterClientUserId(), new TimeOnly(11, 0)));
            var second = BookableBusinessFactory.PostAppointmentAsync(client, setup.OwnerToken,
                Booking(setup, BookableBusinessFactory.CounterClientUserId(), new TimeOnly(11, 0)));

            var responses = await Task.WhenAll(first, second);

            Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
            var loser = Assert.Single(responses, r => r.StatusCode == HttpStatusCode.BadRequest);
            var error = await loser.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("APPOINTMENT_CONFLICT", error!.Code);

            await using var db = _factory.CreateDbContext();
            Assert.Equal(1, await db.Appointments.CountAsync());
        }
    }
}
