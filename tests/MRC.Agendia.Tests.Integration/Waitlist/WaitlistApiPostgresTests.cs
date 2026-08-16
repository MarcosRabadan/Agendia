using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Waitlist.DTO;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Waitlist
{
    /// <summary>
    /// The waitlist join race through the API against a REAL PostgreSQL. The "already
    /// waiting" pre-check is a check-then-act, so two overlapping joins can both pass it;
    /// the filtered unique index (with NULLS NOT DISTINCT, so "any employee" entries
    /// dedupe too) is the backstop, and its violation is translated into a domain error
    /// in the UnitOfWork - the client must see 400 DUPLICATE_WAITLIST_ENTRY, never a 500.
    ///
    /// Neither the index nor the translation exists on EF InMemory, so this can only be
    /// asserted here. Skipped automatically when Docker is unavailable.
    /// </summary>
    [Collection(PostgresApiCollection.Name)]
    public class WaitlistApiPostgresTests
    {
        private const int Year = 2035;
        private static readonly DateOnly SlotDate = new(Year, 6, 4);
        private static readonly TimeOnly SlotTime = new(10, 0);

        private readonly PostgresWebApplicationFactory _factory;

        public WaitlistApiPostgresTests(PostgresWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [SkippableFact]
        public async Task Concurrent_joins_of_the_same_slot_leave_a_single_entry()
        {
            Skip.IfNot(_factory.Available, "Docker/Postgres not available; test skipped.");
            await _factory.ResetDatabaseAsync();

            var client = _factory.CreateClient();
            var setup = await BookableBusinessFactory.CreateAsync(client, _factory.Services, "pg-waitlist", Year);
            var waiting = TestProvisioning.ProvisionClient("pg-waitlist");

            // Fill the slot (the only employee has capacity 1) so the waitlist opens up.
            var start = SlotDate.ToDateTime(SlotTime);
            (await BookableBusinessFactory.PostAppointmentAsync(client, setup.OwnerToken,
                new CreateAppointmentDto(setup.ClientUserId, setup.EmployeeId, setup.Service.Id,
                    start, start.AddMinutes(30), null))).EnsureSuccessStatusCode();

            // Same client, same slot, twice at once - "any employee" (null), which is the
            // case Postgres would treat as distinct without NULLS NOT DISTINCT.
            var dto = new JoinWaitlistDto(setup.BusinessId, setup.Service.Id, SlotDate, SlotTime, EmployeeId: null);
            var responses = await Task.WhenAll(
                BookableBusinessFactory.SendAsync(client, HttpMethod.Post, "/api/waitlist", waiting.Token, dto),
                BookableBusinessFactory.SendAsync(client, HttpMethod.Post, "/api/waitlist", waiting.Token, dto));

            Assert.Single(responses, r => r.StatusCode == HttpStatusCode.OK);
            var rejected = Assert.Single(responses, r => r.StatusCode == HttpStatusCode.BadRequest);
            var error = await rejected.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("DUPLICATE_WAITLIST_ENTRY", error!.Code);

            await using var db = _factory.CreateDbContext();
            Assert.Equal(1, await db.WaitlistEntries.CountAsync());
        }
    }
}
