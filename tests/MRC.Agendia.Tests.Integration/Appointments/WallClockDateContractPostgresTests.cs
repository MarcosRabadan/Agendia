using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Appointments
{
    /// <summary>
    /// Wire contract of the agenda's wall-clock dates (#290): an appointment's
    /// StartDate/EndDate and an employee time-off range are wall-clock times, so they must
    /// arrive WITHOUT a time zone. A value carrying "Z" or an offset is rejected with a
    /// clean 400 instead of reaching persistence.
    ///
    /// Runs against a REAL PostgreSQL on purpose: what this guards is invisible on the
    /// InMemory provider. A body with "Z" deserializes to <c>Kind=Utc</c>, and Npgsql
    /// refuses to write that into a `timestamp without time zone` column, so this path
    /// used to 500 at the INSERT. InMemory ignores <c>DateTime.Kind</c> entirely and would
    /// report a false green. The query-string case rides along here to keep the whole
    /// contract in one readable place.
    ///
    /// Skipped automatically when Docker is unavailable.
    /// </summary>
    [Collection(PostgresApiCollection.Name)]
    public class WallClockDateContractPostgresTests
    {
        private const int Year = 2035;
        private static readonly DateOnly Day = new(Year, 6, 4);

        private readonly PostgresWebApplicationFactory _factory;

        public WallClockDateContractPostgresTests(PostgresWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [SkippableTheory]
        [InlineData("Z")]
        [InlineData("+02:00")]
        public async Task Booking_with_a_zoned_start_date_is_rejected(string zone)
        {
            Skip.IfNot(_factory.Available, "Docker/Postgres not available; test skipped.");
            await _factory.ResetDatabaseAsync();

            var client = _factory.CreateClient();
            var setup = await BookableBusinessFactory.CreateAsync(client, _factory.Services, "pg-wallclock", Year);

            var response = await PostRawAsync(client, "/api/Appointment", setup.OwnerToken, $$"""
                {
                  "clientUserId": "{{setup.ClientUserId}}",
                  "employeeId": "{{setup.EmployeeId}}",
                  "serviceId": "{{setup.Service.Id}}",
                  "startDate": "{{Day:yyyy-MM-dd}}T09:00:00{{zone}}",
                  "endDate": "{{Day:yyyy-MM-dd}}T09:30:00{{zone}}"
                }
                """);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("VALIDATION_ERROR", error!.Code);

            // Nothing may have been persisted: the request is rejected before the handler.
            await using var db = _factory.CreateDbContext();
            Assert.Empty(await db.Appointments.ToListAsync());
        }

        [SkippableTheory]
        [InlineData("Z")]
        [InlineData("+02:00")]
        public async Task Time_off_with_a_zoned_range_is_rejected(string zone)
        {
            Skip.IfNot(_factory.Available, "Docker/Postgres not available; test skipped.");
            await _factory.ResetDatabaseAsync();

            var client = _factory.CreateClient();
            var setup = await BookableBusinessFactory.CreateAsync(client, _factory.Services, "pg-wallclock-off", Year);

            var response = await PostRawAsync(client,
                $"/api/employees/{setup.EmployeeId}/time-off", setup.OwnerToken, $$"""
                {
                  "start": "{{Day:yyyy-MM-dd}}T10:00:00{{zone}}",
                  "end": "{{Day:yyyy-MM-dd}}T13:00:00{{zone}}"
                }
                """);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("VALIDATION_ERROR", error!.Code);

            await using var db = _factory.CreateDbContext();
            Assert.Empty(await db.EmployeeTimeOffs.ToListAsync());
        }

        /// <summary>
        /// The query string does not go through System.Text.Json but through the type
        /// converter, which turns "09:00Z" into <c>Kind=Local</c> shifted to the server's
        /// offset. That never threw: it silently answered for a different window, which is
        /// why this case needs its own guard.
        /// </summary>
        [SkippableFact]
        public async Task Date_range_query_with_a_zoned_bound_is_rejected()
        {
            Skip.IfNot(_factory.Available, "Docker/Postgres not available; test skipped.");
            await _factory.ResetDatabaseAsync();

            var client = _factory.CreateClient();
            var setup = await BookableBusinessFactory.CreateAsync(client, _factory.Services, "pg-wallclock-range", Year);

            var response = await BookableBusinessFactory.SendAsync(client, HttpMethod.Get,
                $"/api/Appointment/business/{setup.BusinessId}" +
                $"?startDate={Day:yyyy-MM-dd}T00:00:00Z&endDate={Day:yyyy-MM-dd}T23:59:59Z",
                setup.OwnerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("VALIDATION_ERROR", error!.Code);
        }

        /// <summary>Positive control: the same range without a zone keeps working.</summary>
        [SkippableFact]
        public async Task Date_range_query_without_a_zone_still_works()
        {
            Skip.IfNot(_factory.Available, "Docker/Postgres not available; test skipped.");
            await _factory.ResetDatabaseAsync();

            var client = _factory.CreateClient();
            var setup = await BookableBusinessFactory.CreateAsync(client, _factory.Services, "pg-wallclock-ok", Year);

            var response = await BookableBusinessFactory.SendAsync(client, HttpMethod.Get,
                $"/api/Appointment/business/{setup.BusinessId}" +
                $"?startDate={Day:yyyy-MM-dd}T00:00:00&endDate={Day:yyyy-MM-dd}T23:59:59",
                setup.OwnerToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // Hand-written JSON on purpose: serializing a typed DTO would emit a DateTime with
        // Kind=Unspecified and never reproduce what a real client sends.
        private static async Task<HttpResponseMessage> PostRawAsync(HttpClient client,
                                                                    string url,
                                                                    string token,
                                                                    string json)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await client.SendAsync(request);
        }
    }
}
