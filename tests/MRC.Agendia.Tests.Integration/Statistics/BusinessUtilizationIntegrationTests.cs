using System.Net;
using System.Net.Http.Json;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Statistics.DTO;
using MRC.Agendia.Application.TimeOff.DTO;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Statistics
{
    /// <summary>
    /// End-to-end coverage for the agenda utilization report (#269): the offered capacity
    /// comes from the real effective schedule, the booked minutes from real appointments,
    /// time off lowers what was on offer, and only the business's own staff can read it.
    ///
    /// The calendar is a full week 09:00-18:00 (see <see cref="BookableBusinessFactory"/>),
    /// so a single day offers 540 minutes per employee.
    /// </summary>
    public class BusinessUtilizationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const int Year = 2035;
        private const int OpenMinutesPerDay = 540;
        private static readonly DateOnly Day = new(Year, 6, 4);

        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public BusinessUtilizationIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Reports_the_occupancy_of_a_booked_day()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "util-basic", Year);
            await BookAsync(setup, new TimeOnly(10, 0));
            await BookAsync(setup, new TimeOnly(11, 0));

            var result = await GetUtilizationAsync(setup.OwnerToken, setup.BusinessId);

            Assert.Equal(OpenMinutesPerDay, result.OfferedMinutes);
            Assert.Equal(60, result.BookedMinutes);
            Assert.Equal(Math.Round(60d / OpenMinutesPerDay, 4), result.OccupancyRate);

            // The booked minutes land on the hours they were booked at.
            Assert.Equal(30, result.ByHour.Single(h => h.Hour == 10).BookedMinutes);
            Assert.Equal(30, result.ByHour.Single(h => h.Hour == 11).BookedMinutes);
            Assert.Equal(0, result.ByHour.Single(h => h.Hour == 9).BookedMinutes);

            // One employee, one weekday in range.
            Assert.Equal(setup.EmployeeId, Assert.Single(result.ByEmployee).EmployeeId);
            Assert.Equal(Day.DayOfWeek, Assert.Single(result.ByWeekday).Weekday);
        }

        [Fact]
        public async Task Time_off_lowers_the_offered_capacity()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "util-timeoff", Year);

            // The employee is away 09:00-13:00: four hours off the offer.
            var timeOff = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post,
                $"/api/employees/{setup.EmployeeId}/time-off", setup.OwnerToken,
                new CreateEmployeeTimeOffDto(Day.ToDateTime(new TimeOnly(9, 0)), Day.ToDateTime(new TimeOnly(13, 0))));
            timeOff.EnsureSuccessStatusCode();

            var result = await GetUtilizationAsync(setup.OwnerToken, setup.BusinessId);

            Assert.Equal(OpenMinutesPerDay - 240, result.OfferedMinutes);
            Assert.DoesNotContain(result.ByHour, h => h.Hour is 9 or 10 or 11 or 12);
        }

        [Fact]
        public async Task Lead_time_counts_how_far_ahead_the_booking_was_made()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "util-lead", Year);
            await BookAsync(setup, new TimeOnly(10, 0));

            var result = await GetUtilizationAsync(setup.OwnerToken, setup.BusinessId);

            // The appointment is years away and was just created, so the lead time is a
            // large positive number - what matters is that the two time worlds were lined
            // up before subtracting (a timezone slip would not show at this scale, but a
            // sign error would).
            Assert.True(result.AvgLeadTimeHours > 0, "Booking ahead of time must yield a positive lead time.");
        }

        [Fact]
        public async Task Staff_of_another_business_is_rejected()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "util-owner", Year);
            var stranger = await TestProvisioning.ProvisionOwnerAsync(_client, "util-stranger");

            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Get,
                Url(setup.BusinessId), stranger.Token);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task A_range_longer_than_the_cap_is_rejected()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "util-range", Year);

            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Get,
                $"/api/businesses/{setup.BusinessId}/stats/utilization?from={Year}-01-01&to={Year}-12-31",
                setup.OwnerToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // ----- Helpers -----

        private static string Url(Guid businessId) =>
            $"/api/businesses/{businessId}/stats/utilization?from={Day:yyyy-MM-dd}&to={Day:yyyy-MM-dd}";

        private async Task BookAsync(BookableBusiness setup, TimeOnly at)
        {
            var start = Day.ToDateTime(at);
            var response = await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                new CreateAppointmentDto(BookableBusinessFactory.CounterClientUserId(), setup.EmployeeId,
                    setup.Service.Id, start, start.AddMinutes(30), null));
            response.EnsureSuccessStatusCode();
        }

        private async Task<UtilizationDto> GetUtilizationAsync(string token, Guid businessId)
        {
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Get, Url(businessId), token);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<UtilizationDto>())!;
        }
    }
}
