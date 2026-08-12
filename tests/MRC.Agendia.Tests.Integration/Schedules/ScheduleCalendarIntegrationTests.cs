using System.Net.Http.Json;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Schedules
{
    /// <summary>
    /// End-to-end coverage of the resolved calendar: after generating a full-week
    /// schedule and closing one day with an override, the calendar endpoint reports
    /// that day as closed and its neighbours as open with time slots.
    /// </summary>
    public class ScheduleCalendarIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const int Year = 2035;
        private static readonly DateOnly ClosedDay = new(Year, 6, 4);

        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ScheduleCalendarIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Calendar_reflects_the_template_and_a_closed_override()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "cal", Year);

            var closed = new CreateScheduleOverrideDto(setup.BusinessId, ClosedDay, ScheduleOverrideType.Closed, "Cerrado", null);
            (await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post,
                $"/api/businesses/{setup.BusinessId}/schedules/overrides", setup.OwnerToken, closed)).EnsureSuccessStatusCode();

            var from = ClosedDay.AddDays(-1);
            var to = ClosedDay.AddDays(1);
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Get,
                $"/api/businesses/{setup.BusinessId}/schedules/calendar?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", setup.OwnerToken);
            response.EnsureSuccessStatusCode();

            var days = await response.Content.ReadFromJsonAsync<List<CalendarDayDto>>();
            Assert.NotNull(days);
            Assert.Equal(3, days!.Count);

            var closedDay = days.Single(d => d.Date == ClosedDay);
            Assert.False(closedDay.IsOpen);

            foreach (var open in days.Where(d => d.Date != ClosedDay))
            {
                Assert.True(open.IsOpen);
                Assert.NotNull(open.TimeSlots);
                Assert.NotEmpty(open.TimeSlots!);
            }
        }
    }
}
