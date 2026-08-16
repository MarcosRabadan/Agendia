using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Api.Filters;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Appointments
{
    /// <summary>
    /// The idempotency claim under a REAL race, through the API against a real PostgreSQL:
    /// two identical bookings sent at once with the same <c>Idempotency-Key</c>. The key is
    /// the table's primary key, so exactly one INSERT survives and the twin is told the
    /// request is already in flight (or replays it) - never a second appointment.
    ///
    /// EF InMemory cannot show this: without a shared database there is no race to lose.
    /// Skipped automatically when Docker is unavailable.
    /// </summary>
    [Collection(PostgresApiCollection.Name)]
    public class IdempotentBookingPostgresTests
    {
        private const int Year = 2035;
        private static readonly DateOnly Day = new(Year, 6, 4);

        private readonly PostgresWebApplicationFactory _factory;

        public IdempotentBookingPostgresTests(PostgresWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [SkippableFact]
        public async Task Concurrent_retries_with_the_same_key_create_a_single_appointment()
        {
            Skip.IfNot(_factory.Available, "Docker/Postgres not available; test skipped.");
            await _factory.ResetDatabaseAsync();

            var client = _factory.CreateClient();
            var setup = await BookableBusinessFactory.CreateAsync(client, _factory.Services, "pg-idem", Year);
            var key = Guid.NewGuid().ToString();
            var start = Day.ToDateTime(new TimeOnly(9, 0));
            var booking = new CreateAppointmentDto(setup.ClientUserId, setup.EmployeeId, setup.Service.Id,
                start, start.AddMinutes(30), null);

            var responses = await Task.WhenAll(
                PostAsync(client, setup.OwnerToken, booking, key),
                PostAsync(client, setup.OwnerToken, booking, key));

            // One of them owns the key and books. Depending on how the two overlap, the
            // twin either finds the claim still in flight (409) or replays the finished
            // answer (201) - but never books a second appointment.
            Assert.All(responses, r => Assert.True(
                r.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict,
                $"Unexpected status for a concurrent twin: {r.StatusCode}."));

            foreach (var conflict in responses.Where(r => r.StatusCode == HttpStatusCode.Conflict))
            {
                var error = await conflict.Content.ReadFromJsonAsync<ApiError>();
                Assert.Equal("IDEMPOTENT_REQUEST_IN_PROGRESS", error!.Code);
            }

            var booked = new List<Guid>();
            foreach (var created in responses.Where(r => r.StatusCode == HttpStatusCode.Created))
                booked.Add((await created.Content.ReadFromJsonAsync<AppointmentDto>())!.Id);

            // If both answered 201 it is because the second one replayed the first.
            Assert.NotEmpty(booked);
            Assert.Single(booked.Distinct());

            await using var db = _factory.CreateDbContext();
            Assert.Equal(1, await db.Appointments.CountAsync());
        }

        private static async Task<HttpResponseMessage> PostAsync(HttpClient client,
                                                                 string token,
                                                                 CreateAppointmentDto dto,
                                                                 string key)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Appointment")
            {
                Content = JsonContent.Create(dto)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add(IdempotencyFilter.HeaderName, key);
            return await client.SendAsync(request);
        }
    }
}
