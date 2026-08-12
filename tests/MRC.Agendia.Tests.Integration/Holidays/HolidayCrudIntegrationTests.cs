using System.Net;
using System.Net.Http.Json;
using MRC.Agendia.Application.Holidays.DTO;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Holidays
{
    /// <summary>
    /// End-to-end CRUD of the holiday calendar (Admin-only writes): create, read,
    /// update and delete round-trip; the year/date consistency rule is enforced (400);
    /// and a non-admin caller cannot create (403).
    /// </summary>
    public class HolidayCrudIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public HolidayCrudIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private static string AdminToken() => TestTokenFactory.Create($"admin-{Guid.NewGuid():N}", Roles.Admin);

        [Fact]
        public async Task Admin_can_create_read_update_and_delete_a_holiday()
        {
            var token = AdminToken();
            var create = new CreateHolidayCalendarDto(new DateOnly(2035, 5, 1), "Dia del trabajo", HolidayScope.National, 2035);

            var createResp = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post, "/api/Holiday", token, create);
            Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
            var created = await createResp.Content.ReadFromJsonAsync<HolidayCalendarDto>();
            Assert.NotNull(created);

            var get = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Get, $"/api/Holiday/{created!.Id}", token);
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);

            var update = new UpdateHolidayCalendarDto(created.Id, created.Date, "Fiesta del trabajo", HolidayScope.National, 2035);
            var updateResp = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Put, $"/api/Holiday/{created.Id}", token, update);
            Assert.Equal(HttpStatusCode.OK, updateResp.StatusCode);
            var updated = await updateResp.Content.ReadFromJsonAsync<HolidayCalendarDto>();
            Assert.Equal("Fiesta del trabajo", updated!.Name);

            var deleteResp = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Delete, $"/api/Holiday/{created.Id}", token);
            Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

            var afterDelete = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Get, $"/api/Holiday/{created.Id}", token);
            Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
        }

        [Fact]
        public async Task Create_with_date_not_in_year_is_rejected()
        {
            var token = AdminToken();
            var create = new CreateHolidayCalendarDto(new DateOnly(2035, 5, 1), "Desfase", HolidayScope.National, 2036);

            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post, "/api/Holiday", token, create);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("VALIDATION_ERROR", error!.Code);
        }

        [Fact]
        public async Task Non_admin_cannot_create_a_holiday()
        {
            var clientToken = TestTokenFactory.Create($"client-{Guid.NewGuid():N}", Roles.Client);
            var create = new CreateHolidayCalendarDto(new DateOnly(2035, 5, 1), "Dia", HolidayScope.National, 2035);

            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post, "/api/Holiday", clientToken, create);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
