using System.Net.Http.Json;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Availability.DTO;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Appointments
{
    /// <summary>
    /// End-to-end coverage of the availability endpoint: an open day yields sized
    /// slots with capacity, a Closed override day yields no slots, and booking a slot
    /// consumes its capacity (the only employee, capacity 1).
    /// </summary>
    public class AvailabilityIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const int Year = 2035;
        private static readonly DateOnly Day = new(Year, 6, 4);

        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AvailabilityIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        private async Task<AvailabilityDto> GetAvailabilityAsync(int businessId, int serviceId, DateOnly date, int step = 30)
        {
            var response = await _client.GetAsync(
                $"/api/businesses/{businessId}/availability?date={date:yyyy-MM-dd}&serviceId={serviceId}&stepMinutes={step}");
            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<AvailabilityDto>();
            Assert.NotNull(dto);
            return dto!;
        }

        [Fact]
        public async Task Open_day_returns_sized_slots_with_capacity()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "avail-open", Year, durationMinutes: 30);

            var availability = await GetAvailabilityAsync(setup.BusinessId, setup.Service.Id, Day);

            Assert.True(availability.IsOpen);
            Assert.Equal(30, availability.DurationMinutes);
            Assert.NotEmpty(availability.Slots);
            Assert.All(availability.Slots, s => Assert.Equal(30, (s.EndTime - s.StartTime).TotalMinutes));
            Assert.All(availability.Slots, s => Assert.True(s.Capacity >= 1));
            Assert.Contains(availability.Slots, s => s.StartTime == new TimeOnly(9, 0));
        }

        [Fact]
        public async Task Closed_override_day_returns_no_slots()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "avail-closed", Year, durationMinutes: 30);

            var closed = new CreateScheduleOverrideDto(setup.BusinessId, Day, ScheduleOverrideType.Closed, "Cerrado", null);
            (await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post,
                $"/api/businesses/{setup.BusinessId}/schedules/overrides", setup.OwnerToken, closed)).EnsureSuccessStatusCode();

            var availability = await GetAvailabilityAsync(setup.BusinessId, setup.Service.Id, Day);

            Assert.False(availability.IsOpen);
            Assert.Empty(availability.Slots);
        }

        [Fact]
        public async Task Booking_a_slot_consumes_its_capacity()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "avail-book", Year, durationMinutes: 30);

            var before = await GetAvailabilityAsync(setup.BusinessId, setup.Service.Id, Day);
            Assert.Contains(before.Slots, s => s.StartTime == new TimeOnly(10, 0) && s.Capacity >= 1);

            var start = Day.ToDateTime(new TimeOnly(10, 0));
            var booking = new CreateAppointmentDto(setup.ClientUserId, setup.EmployeeId, setup.Service.Id, start, start.AddMinutes(30), null);
            (await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken, booking)).EnsureSuccessStatusCode();

            var after = await GetAvailabilityAsync(setup.BusinessId, setup.Service.Id, Day);

            // The only employee (capacity 1) is now busy at 10:00, so no bookable slot there.
            Assert.DoesNotContain(after.Slots, s => s.StartTime == new TimeOnly(10, 0) && s.Capacity >= 1);
            // Other slots stay open.
            Assert.Contains(after.Slots, s => s.StartTime == new TimeOnly(11, 0) && s.Capacity >= 1);
        }
    }
}
