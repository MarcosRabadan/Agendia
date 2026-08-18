using System.Net;
using System.Net.Http.Json;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Availability.DTO;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Application.Waitlist.DTO;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Waitlist
{
    /// <summary>
    /// End-to-end parity between what the API OFFERS and what it ACCEPTS while a waitlist
    /// hold is running (#308). A hold reserves ONE seat for ONE client, so a group class
    /// keeps offering its spare seats and an "any employee" hold costs the business one
    /// seat rather than every employee. These are the flows that used to disagree:
    /// availability hid seats the validator would have taken, or published seats the
    /// validator then refused.
    /// </summary>
    public class WaitlistHoldParityIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const int Year = 2035;
        private static readonly DateOnly SlotDate = new(Year, 6, 4);
        private static readonly TimeOnly SlotTime = new(10, 0);

        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public WaitlistHoldParityIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task A_group_class_keeps_offering_the_seats_that_are_not_held()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "hold-parity-group", Year);
            var teacher = await CreateEmployeeAsync(setup, maxConcurrent: 3);

            // Fill the group class, queue a client, then free two of the three seats.
            var booked = new List<AppointmentDto>();
            for (var i = 0; i < 3; i++)
                booked.Add(await BookAsync(setup, teacher, BookableBusinessFactory.CounterClientUserId()));

            var waiting = TestProvisioning.ProvisionClient("hold-parity-group-wait");
            await JoinAsync(setup, teacher, waiting.Token);

            await CancelAsync(setup, booked[0]);
            await CancelAsync(setup, booked[1]);

            // One seat is still taken and one is held for the waiting client, so exactly one
            // is free. Dropping the whole employee, as this used to, reported the class full.
            var other = TestProvisioning.ProvisionClient("hold-parity-group-other");
            var slot = (await GetAvailabilityAsync(setup, teacher, other.Token))
                .Slots.Single(s => s.StartTime == SlotTime);
            Assert.Equal(1, slot.Capacity);

            // And what is offered is really accepted.
            var response = await BookableBusinessFactory.PostAppointmentAsync(_client, other.Token,
                Booking(setup, teacher, other.UserId));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task An_any_employee_hold_costs_the_business_one_seat_not_every_employee()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "hold-parity-any", Year);
            var second = await CreateEmployeeAsync(setup, maxConcurrent: 1);
            var third = await CreateEmployeeAsync(setup, maxConcurrent: 1);

            // Every teacher busy at the slot, with one client queued for "any of them".
            await BookAsync(setup, setup.EmployeeId, BookableBusinessFactory.CounterClientUserId());
            var onSecond = await BookAsync(setup, second, BookableBusinessFactory.CounterClientUserId());
            var onThird = await BookAsync(setup, third, BookableBusinessFactory.CounterClientUserId());

            var waiting = TestProvisioning.ProvisionClient("hold-parity-any-wait");
            await JoinAsync(setup, employeeId: null, waiting.Token);

            await CancelAsync(setup, onSecond);
            await CancelAsync(setup, onThird);

            // Two seats freed and one of them held: the business has exactly one left to sell.
            var buyer = TestProvisioning.ProvisionClient("hold-parity-any-buyer");
            var slot = (await GetAvailabilityAsync(setup, employeeId: null, buyer.Token))
                .Slots.Single(s => s.StartTime == SlotTime);
            Assert.Equal(1, slot.Capacity);

            // The offered seat is genuinely bookable. Counting the single hold against every
            // employee, as this used to, answered SLOT_ON_HOLD to all of them.
            var taken = await BookableBusinessFactory.PostAppointmentAsync(_client, buyer.Token,
                Booking(setup, second, buyer.UserId));
            Assert.Equal(HttpStatusCode.Created, taken.StatusCode);

            // And the last seat stays reserved for whoever is holding it.
            var latecomer = TestProvisioning.ProvisionClient("hold-parity-any-late");
            var refused = await BookableBusinessFactory.PostAppointmentAsync(_client, latecomer.Token,
                Booking(setup, third, latecomer.UserId));

            Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
            var error = await refused.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("SLOT_ON_HOLD", error!.Code);
        }

        [Fact]
        public async Task Another_client_can_still_join_the_queue_while_someone_holds_the_slot()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "hold-parity-join", Year);
            var appointment = await BookAsync(setup, setup.EmployeeId, BookableBusinessFactory.CounterClientUserId());

            var waiting = TestProvisioning.ProvisionClient("hold-parity-join-wait");
            await JoinAsync(setup, setup.EmployeeId, waiting.Token);

            await CancelAsync(setup, appointment);

            // The slot is held, so it is not free and a second client must be able to queue
            // for it. Ignoring holds in the capacity probe answered "book directly" while the
            // booking itself was refused with SLOT_ON_HOLD, so they could do neither.
            var latecomer = TestProvisioning.ProvisionClient("hold-parity-join-late");
            var join = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post, "/api/waitlist", latecomer.Token,
                new JoinWaitlistDto(setup.BusinessId, setup.Service.Id, SlotDate, SlotTime, setup.EmployeeId));

            Assert.Equal(HttpStatusCode.OK, join.StatusCode);
        }

        // ----- Helpers -----

        private static CreateAppointmentDto Booking(BookableBusiness setup, Guid employeeId, string clientUserId)
        {
            var start = SlotDate.ToDateTime(SlotTime);
            return new CreateAppointmentDto(clientUserId, employeeId, setup.Service.Id,
                start, start.AddMinutes(30), null);
        }

        private async Task<AppointmentDto> BookAsync(BookableBusiness setup, Guid employeeId, string clientUserId)
        {
            var response = await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                Booking(setup, employeeId, clientUserId));
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AppointmentDto>())!;
        }

        private async Task<Guid> CreateEmployeeAsync(BookableBusiness setup, int maxConcurrent)
        {
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post, "/api/Employee",
                setup.OwnerToken, new CreateEmployeeDto(setup.BusinessId, null, maxConcurrent));
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<EmployeeDto>())!.Id;
        }

        private async Task JoinAsync(BookableBusiness setup, Guid? employeeId, string token)
        {
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post, "/api/waitlist", token,
                new JoinWaitlistDto(setup.BusinessId, setup.Service.Id, SlotDate, SlotTime, employeeId));
            response.EnsureSuccessStatusCode();
        }

        private async Task CancelAsync(BookableBusiness setup, AppointmentDto appointment)
        {
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Delete,
                $"/api/Appointment/{appointment.Id}", setup.OwnerToken);
            response.EnsureSuccessStatusCode();
        }

        private async Task<AvailabilityDto> GetAvailabilityAsync(BookableBusiness setup, Guid? employeeId, string token)
        {
            var url = $"/api/businesses/{setup.BusinessId}/availability?date={SlotDate:yyyy-MM-dd}"
                      + $"&serviceId={setup.Service.Id}&stepMinutes=30";
            if (employeeId is Guid id)
                url += $"&employeeId={id}";

            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Get, url, token);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AvailabilityDto>())!;
        }
    }
}
