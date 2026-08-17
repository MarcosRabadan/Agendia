using System.Net;
using System.Net.Http.Json;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Availability.DTO;
using MRC.Agendia.Application.TimeOff.DTO;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Appointments
{
    /// <summary>
    /// End-to-end coverage for employee time off (#271): the blocked slots disappear from
    /// THAT employee's availability without touching the rest of the staff, booking on top
    /// of a block is rejected with a typed error, appointments already inside the range are
    /// reported but left alone, and removing the block frees the slots again.
    /// </summary>
    public class EmployeeTimeOffIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const int Year = 2035;
        private static readonly DateOnly Day = new(Year, 6, 4);

        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public EmployeeTimeOffIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Booking_inside_a_block_is_rejected()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "off-book", Year);
            await CreateTimeOffAsync(setup.OwnerToken, setup.EmployeeId, new TimeOnly(10, 0), new TimeOnly(13, 0));

            var response = await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                Booking(setup, new TimeOnly(11, 0)));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var error = await response.Content.ReadFromJsonAsync<ApiError>();
            Assert.Equal("EMPLOYEE_UNAVAILABLE", error!.Code);
        }

        [Fact]
        public async Task Booking_outside_the_block_still_works()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "off-edge", Year);
            await CreateTimeOffAsync(setup.OwnerToken, setup.EmployeeId, new TimeOnly(10, 0), new TimeOnly(13, 0));

            // The range is half-open: an appointment starting exactly at 13:00 is fine,
            // and so is one ending exactly at 10:00.
            var atTheEnd = await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                Booking(setup, new TimeOnly(13, 0)));
            var justBefore = await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                Booking(setup, new TimeOnly(9, 30)));

            Assert.Equal(HttpStatusCode.Created, atTheEnd.StatusCode);
            Assert.Equal(HttpStatusCode.Created, justBefore.StatusCode);
        }

        [Fact]
        public async Task Blocked_slots_disappear_from_that_employees_availability()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "off-avail", Year);

            var before = await GetAvailabilityAsync(setup);
            Assert.Contains(before.Slots, s => s.StartTime == new TimeOnly(11, 0));

            await CreateTimeOffAsync(setup.OwnerToken, setup.EmployeeId, new TimeOnly(10, 0), new TimeOnly(13, 0));

            var during = await GetAvailabilityAsync(setup);
            Assert.DoesNotContain(during.Slots, s => s.StartTime == new TimeOnly(11, 0));
            // Outside the block the day is untouched.
            Assert.Contains(during.Slots, s => s.StartTime == new TimeOnly(9, 0));
            Assert.Contains(during.Slots, s => s.StartTime == new TimeOnly(13, 0));
        }

        [Fact]
        public async Task Another_employee_is_not_affected()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "off-other", Year);
            var colleague = await CreateColleagueAsync(setup);

            await CreateTimeOffAsync(setup.OwnerToken, setup.EmployeeId, new TimeOnly(10, 0), new TimeOnly(13, 0));

            // The business-wide availability still offers 11:00, now only with the colleague.
            var availability = await GetAvailabilityAsync(setup, wholeBusiness: true);
            var slot = availability.Slots.Single(s => s.StartTime == new TimeOnly(11, 0));
            Assert.Equal(new[] { colleague }, slot.AvailableEmployeeIds.ToArray());

            // And booking that colleague at 11:00 goes through.
            var response = await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                Booking(setup, new TimeOnly(11, 0)) with { EmployeeId = colleague });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Existing_appointments_are_reported_but_left_alone()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "off-collision", Year);
            var booked = await (await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                Booking(setup, new TimeOnly(11, 0)))).Content.ReadFromJsonAsync<AppointmentDto>();

            var result = await CreateTimeOffAsync(setup.OwnerToken, setup.EmployeeId, new TimeOnly(10, 0), new TimeOnly(13, 0));

            Assert.Equal(new[] { booked!.Id }, result.CollidingAppointmentIds.ToArray());

            // The appointment is still there, untouched: the block only stops NEW bookings.
            var stillThere = await BookableBusinessFactory.GetAppointmentAsync(_client, setup.OwnerToken, booked.Id);
            Assert.Equal(booked.Status, stillThere.Status);
        }

        [Fact]
        public async Task Removing_the_block_frees_the_slots_again()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "off-delete", Year);
            var created = await CreateTimeOffAsync(setup.OwnerToken, setup.EmployeeId, new TimeOnly(10, 0), new TimeOnly(13, 0));

            var listed = await GetTimeOffAsync(setup.OwnerToken, setup.EmployeeId);
            Assert.Single(listed);

            var delete = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Delete,
                $"{TimeOffUrl(setup.EmployeeId)}/{created.TimeOff.Id}", setup.OwnerToken);
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

            var response = await BookableBusinessFactory.PostAppointmentAsync(_client, setup.OwnerToken,
                Booking(setup, new TimeOnly(11, 0)));
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Staff_of_another_business_cannot_block_an_employee()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "off-owner", Year);
            var stranger = await TestProvisioning.ProvisionOwnerAsync(_client, "off-stranger");

            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post,
                TimeOffUrl(setup.EmployeeId), stranger.Token,
                new CreateEmployeeTimeOffDto(Day.ToDateTime(new TimeOnly(10, 0)), Day.ToDateTime(new TimeOnly(13, 0))));

            // 404, the cross-tenant convention (R7): a stranger is not told the employee
            // exists. What matters is that the block is not created.
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Empty(await GetTimeOffAsync(setup.OwnerToken, setup.EmployeeId));
        }

        [Fact]
        public async Task An_inverted_range_is_rejected()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "off-range", Year);

            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post,
                TimeOffUrl(setup.EmployeeId), setup.OwnerToken,
                new CreateEmployeeTimeOffDto(Day.ToDateTime(new TimeOnly(13, 0)), Day.ToDateTime(new TimeOnly(10, 0))));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        /// <summary>
        /// A block on ONE date of a recurring series only costs that occurrence (#291). The
        /// block is deliberately on the SECOND occurrence: before the fix the first one was
        /// already created and committed when the second aborted the whole request, leaving a
        /// half-booked series its author never saw.
        /// </summary>
        [Fact]
        public async Task A_series_skips_only_the_occurrence_inside_a_block()
        {
            var setup = await BookableBusinessFactory.CreateAsync(_client, _factory.Services, "off-series", Year);
            var blockedDay = Day.AddDays(7);

            var block = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post,
                TimeOffUrl(setup.EmployeeId), setup.OwnerToken,
                new CreateEmployeeTimeOffDto(
                    blockedDay.ToDateTime(new TimeOnly(10, 0)),
                    blockedDay.ToDateTime(new TimeOnly(13, 0)),
                    "Doctor"));
            block.EnsureSuccessStatusCode();

            // Weekly at 11:00 on Day, Day+7 (blocked) and Day+14.
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post,
                "/api/Appointment/series", setup.OwnerToken,
                new CreateAppointmentSeriesDto(
                    ClientUserId: setup.ClientUserId,
                    EmployeeId: setup.EmployeeId,
                    ServiceId: setup.Service.Id,
                    StartTime: new TimeOnly(11, 0),
                    Frequency: RecurrenceFrequency.Weekly,
                    Interval: 1,
                    DaysOfWeek: new[] { Day.DayOfWeek },
                    DayOfMonth: null,
                    StartDate: Day,
                    UntilDate: Day.AddDays(14),
                    Notes: null));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<AppointmentSeriesResultDto>();

            Assert.Equal(2, result!.Created.Count);
            Assert.DoesNotContain(result.Created, a => a.StartDate.Date == blockedDay.ToDateTime(TimeOnly.MinValue));
            var skip = Assert.Single(result.Skipped);
            Assert.Equal(blockedDay, skip.Date);
            Assert.Equal("EMPLOYEE_UNAVAILABLE", skip.Code);
        }

        // ----- Helpers -----

        private static string TimeOffUrl(Guid employeeId) => $"/api/employees/{employeeId}/time-off";

        private static CreateAppointmentDto Booking(BookableBusiness setup, TimeOnly at)
        {
            var start = Day.ToDateTime(at);
            return new CreateAppointmentDto(setup.ClientUserId, setup.EmployeeId, setup.Service.Id,
                start, start.AddMinutes(30), null);
        }

        private async Task<CreateEmployeeTimeOffResultDto> CreateTimeOffAsync(string token,
                                                                              Guid employeeId,
                                                                              TimeOnly from,
                                                                              TimeOnly to)
        {
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post,
                TimeOffUrl(employeeId), token,
                new CreateEmployeeTimeOffDto(Day.ToDateTime(from), Day.ToDateTime(to), "Doctor"));
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<CreateEmployeeTimeOffResultDto>())!;
        }

        private async Task<IReadOnlyList<EmployeeTimeOffDto>> GetTimeOffAsync(string token, Guid employeeId)
        {
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Get,
                $"{TimeOffUrl(employeeId)}?from={Day:yyyy-MM-dd}&to={Day:yyyy-MM-dd}", token);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<List<EmployeeTimeOffDto>>())!;
        }

        /// <summary>
        /// The day's availability, either for the owner's own employee or - with
        /// <paramref name="wholeBusiness"/> - across every employee of the business.
        /// </summary>
        private async Task<AvailabilityDto> GetAvailabilityAsync(BookableBusiness setup, bool wholeBusiness = false)
        {
            var url = $"/api/businesses/{setup.BusinessId}/availability?date={Day:yyyy-MM-dd}&serviceId={setup.Service.Id}&stepMinutes=30"
                      + (wholeBusiness ? string.Empty : $"&employeeId={setup.EmployeeId}");
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Get, url, setup.OwnerToken);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<AvailabilityDto>())!;
        }

        private async Task<Guid> CreateColleagueAsync(BookableBusiness setup)
        {
            var response = await BookableBusinessFactory.SendAsync(_client, HttpMethod.Post, "/api/Employee",
                setup.OwnerToken,
                new Application.Employees.DTO.CreateEmployeeDto(BusinessId: setup.BusinessId, UserId: null));
            response.EnsureSuccessStatusCode();
            var employee = await response.Content.ReadFromJsonAsync<Application.Employees.DTO.EmployeeDto>();
            return employee!.Id;
        }
    }
}
