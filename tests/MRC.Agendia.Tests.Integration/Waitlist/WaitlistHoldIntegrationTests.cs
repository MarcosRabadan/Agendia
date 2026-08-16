using System.Net;
using System.Net.Http.Json;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Availability.DTO;
using MRC.Agendia.Application.Waitlist.DTO;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Waitlist
{
    /// <summary>
    /// End-to-end coverage for the waitlist priority hold (#268): when a slot frees up the
    /// first client in the queue gets it reserved for a few minutes - nobody else can take
    /// it, they can, and taking it closes the hold.
    /// </summary>
    public class WaitlistHoldIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const int Year = 2035;
        private static readonly DateOnly SlotDate = new(Year, 6, 4);
        private static readonly TimeOnly SlotTime = new(10, 0);

        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public WaitlistHoldIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Freeing_a_slot_hands_the_waiting_client_a_hold()
        {
            var (setup, waiting, appointment) = await QueuedSlotAsync("hold-grant");

            await CancelAsync(setup, appointment);

            var entry = Assert.Single(await GetMyWaitlistAsync(waiting.Token));
            Assert.Equal(WaitlistStatus.Notified, entry.Status);
            Assert.NotNull(entry.HoldUntil);
            Assert.True(entry.HoldUntil > DateTime.UtcNow, "The hold must still be running right after the notification.");
        }

        [Fact]
        public async Task Nobody_else_can_book_the_held_slot()
        {
            var (setup, _, appointment) = await QueuedSlotAsync("hold-blocked");
            await CancelAsync(setup, appointment);

            var response = await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                Booking(setup, BookableBusinessFactory.CounterClientUserId()));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("SLOT_ON_HOLD", error!.Code);
        }

        [Fact]
        public async Task The_holder_can_book_it_and_that_closes_the_hold()
        {
            var (setup, waiting, appointment) = await QueuedSlotAsync("hold-book");
            await CancelAsync(setup, appointment);

            var response = await BookableBusinessFactory.PostAppointmentAsync(_client, waiting.Token,
                Booking(setup, waiting.UserId));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // The entry leaves the queue as Booked and stops reserving a seat.
            var entry = Assert.Single(await GetMyWaitlistAsync(waiting.Token));
            Assert.Equal(WaitlistStatus.Booked, entry.Status);
            Assert.Null(entry.HoldUntil);
        }

        [Fact]
        public async Task The_held_slot_is_not_offered_to_other_clients()
        {
            var (setup, waiting, appointment) = await QueuedSlotAsync("hold-avail");
            await CancelAsync(setup, appointment);

            // A different caller does not see the slot: it is not theirs to take.
            var other = TestProvisioning.ProvisionClient("hold-avail-other");
            var forOthers = await GetAvailabilityAsync(setup, other.Token);
            Assert.DoesNotContain(forOthers.Slots, s => s.StartTime == SlotTime);

            // The holder does see it, or they could not book what they were just offered.
            var forHolder = await GetAvailabilityAsync(setup, waiting.Token);
            Assert.Contains(forHolder.Slots, s => s.StartTime == SlotTime);
        }

        // ----- Helpers -----

        private static CreateAppointmentDto Booking(BookableBusiness setup, string clientUserId)
        {
            var start = SlotDate.ToDateTime(SlotTime);
            return new CreateAppointmentDto(clientUserId, setup.EmployeeId, setup.Service.Id,
                start, start.AddMinutes(30), null);
        }

        /// <summary>
        /// A business whose only slot at <see cref="SlotTime"/> is taken, with one client
        /// waiting in the queue for it.
        /// </summary>
        private async Task<(BookableBusiness Setup, ProvisionedClient Waiting, AppointmentDto Appointment)>
            QueuedSlotAsync(string slug)
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, slug, Year);
            var appointment = await (await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                Booking(setup, BookableBusinessFactory.CounterClientUserId()))).Content.ReadFromJsonAsync<AppointmentDto>();

            var waiting = TestProvisioning.ProvisionClient(slug);
            var join = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post, "/api/waitlist", waiting.Token,
                new JoinWaitlistDto(setup.BusinessId, setup.Service.Id, SlotDate, SlotTime, setup.EmployeeId));
            join.EnsureSuccessStatusCode();

            return (setup, waiting, appointment!);
        }

        private async Task CancelAsync(BookableBusiness setup, AppointmentDto appointment)
        {
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Delete,
                $"/api/Appointment/{appointment.Id}", setup.OwnerToken);
            response.EnsureSuccessStatusCode();
        }

        private async Task<IReadOnlyList<WaitlistEntryDto>> GetMyWaitlistAsync(string token)
        {
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Get, "/api/waitlist/me", token);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<List<WaitlistEntryDto>>())!;
        }

        private async Task<AvailabilityDto> GetAvailabilityAsync(BookableBusiness setup, string token)
        {
            var url = $"/api/businesses/{setup.BusinessId}/availability?date={SlotDate:yyyy-MM-dd}"
                      + $"&serviceId={setup.Service.Id}&employeeId={setup.EmployeeId}&stepMinutes=30";
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Get, url, token);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AvailabilityDto>())!;
        }
    }
}
