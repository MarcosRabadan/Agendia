using MRC.Agendia.Application.Auditing.Queries;
using MRC.Agendia.Application.Availability.Queries;
using MRC.Agendia.Application.Business.Commands.Delete;
using MRC.Agendia.Application.Business.Commands.Restore;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Employees.Commands.Delete;
using MRC.Agendia.Application.Employees.Commands.Restore;
using MRC.Agendia.Application.Employees.Queries.GetAll;
using MRC.Agendia.Application.Holidays.Commands.Create;
using MRC.Agendia.Application.Holidays.Commands.Delete;
using MRC.Agendia.Application.Holidays.Commands.Update;
using MRC.Agendia.Application.Holidays.DTO;
using MRC.Agendia.Application.ServiceAuth.Commands;
using MRC.Agendia.Application.ServiceAuth.DTO;
using MRC.Agendia.Application.Services.Commands.Delete;
using MRC.Agendia.Application.Services.Commands.Restore;
using MRC.Agendia.Application.TimeOff.Commands;
using MRC.Agendia.Application.TimeOff.DTO;
using MRC.Agendia.Application.Waitlist.Commands.Join;
using MRC.Agendia.Application.Waitlist.Commands.Leave;
using MRC.Agendia.Application.Waitlist.DTO;
using MRC.Agendia.Domain.Enums;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Application.Validators
{
    /// <summary>
    /// Rule coverage for the remaining validators: availability, waitlist (with the
    /// clock-based "future slot" rule), auditing/pagination, holidays (year vs. date),
    /// service-auth credentials, device tokens, employee time off, and the shared id-only
    /// delete/restore and pagination validators of every aggregate.
    /// </summary>
    public class MiscValidatorsTests
    {
        // ---------- Availability ----------

        private static GetAvailabilityQuery ValidAvailability() =>
            new(BusinessId: TestIds.Of(1), Date: new DateOnly(2026, 6, 1), ServiceId: TestIds.Of(1), EmployeeId: null, StepMinutes: 30);

        [Fact]
        public void Availability_valid_passes()
            => new GetAvailabilityQueryValidator().Check(ValidAvailability()).ShouldBeValid();

        [Theory]
        [InlineData(4)]
        [InlineData(121)]
        public void Availability_step_out_of_range_fails(int step)
            => new GetAvailabilityQueryValidator().Check(ValidAvailability() with { StepMinutes = step }).ShouldFailOn("StepMinutes");

        [Fact]
        public void Availability_default_date_fails()
            => new GetAvailabilityQueryValidator().Check(ValidAvailability() with { Date = default }).ShouldFailOn("Date");

        [Fact]
        public void Availability_employee_when_present_must_be_positive()
            => new GetAvailabilityQueryValidator().Check(ValidAvailability() with { EmployeeId = TestIds.Of(0) }).ShouldFailOn("EmployeeId");

        [Fact]
        public void Availability_extra_service_equal_to_principal_fails()
            => new GetAvailabilityQueryValidator()
                .Check(ValidAvailability() with { ServiceId = TestIds.Of(1), ExtraServiceIds = new[] { TestIds.Of(1) } }).ShouldFailOn("");

        [Fact]
        public void Availability_valid_extras_pass()
            => new GetAvailabilityQueryValidator()
                .Check(ValidAvailability() with { ExtraServiceIds = new[] { TestIds.Of(2), TestIds.Of(3) } }).ShouldBeValid();

        // ---------- Waitlist ----------

        private static IClock ClockAt(DateTime now)
        {
            var clock = Substitute.For<IClock>();
            clock.BusinessNow.Returns(now);
            return clock;
        }

        private static JoinWaitlistDto ValidJoin() =>
            new(BusinessId: TestIds.Of(1), ServiceId: TestIds.Of(1), Date: new DateOnly(2026, 6, 1), StartTime: new TimeOnly(10, 0), EmployeeId: null);

        [Fact]
        public void JoinWaitlist_future_slot_passes()
            => new JoinWaitlistCommandValidator(ClockAt(new DateTime(2026, 1, 1)))
                .Check(new JoinWaitlistCommand(ValidJoin())).ShouldBeValid();

        [Fact]
        public void JoinWaitlist_past_slot_fails()
            => new JoinWaitlistCommandValidator(ClockAt(new DateTime(2027, 1, 1)))
                .Check(new JoinWaitlistCommand(ValidJoin())).ShouldFailOn("Dto");

        [Fact]
        public void JoinWaitlist_business_id_must_be_positive()
            => new JoinWaitlistCommandValidator(ClockAt(new DateTime(2026, 1, 1)))
                .Check(new JoinWaitlistCommand(ValidJoin() with { BusinessId = TestIds.Of(0) })).ShouldFailOn("Dto.BusinessId");

        [Fact]
        public void LeaveWaitlist_entry_id_must_be_positive()
            => new LeaveWaitlistCommandValidator().Check(new LeaveWaitlistCommand(TestIds.Of(0))).ShouldFailOn("EntryId");

        // ---------- Holidays ----------

        private static CreateHolidayCalendarDto ValidHoliday() =>
            new(new DateOnly(2026, 5, 1), "Dia del trabajo", HolidayScope.National, 2026);

        [Fact]
        public void CreateHoliday_valid_passes()
            => new CreateHolidayCommandValidator().Check(new CreateHolidayCommand(ValidHoliday())).ShouldBeValid();

        [Fact]
        public void CreateHoliday_date_year_mismatch_fails()
            => new CreateHolidayCommandValidator()
                .Check(new CreateHolidayCommand(ValidHoliday() with { Year = 2025 })).ShouldFailOn("Dto");

        [Fact]
        public void CreateHoliday_empty_name_fails()
            => new CreateHolidayCommandValidator()
                .Check(new CreateHolidayCommand(ValidHoliday() with { Name = "" })).ShouldFailOn("Dto.Name");

        [Fact]
        public void CreateHoliday_scope_out_of_enum_fails()
            => new CreateHolidayCommandValidator()
                .Check(new CreateHolidayCommand(ValidHoliday() with { Scope = (HolidayScope)99 })).ShouldFailOn("Dto.Scope");

        [Fact]
        public void UpdateHoliday_id_must_be_positive()
            => new UpdateHolidayCommandValidator()
                .Check(new UpdateHolidayCommand(new UpdateHolidayCalendarDto(TestIds.Of(0), new DateOnly(2026, 5, 1), "X", HolidayScope.National, 2026)))
                .ShouldFailOn("Dto.Id");

        [Fact]
        public void DeleteHoliday_id_must_be_positive()
            => new DeleteHolidayCommandValidator().Check(new DeleteHolidayCommand(TestIds.Of(0))).ShouldFailOn("Id");

        // ---------- Service auth ----------

        [Fact]
        public void AuthenticateService_valid_passes()
            => new AuthenticateServiceCommandValidator()
                .Check(new AuthenticateServiceCommand(new ServiceTokenRequestDto("svc", "secret"))).ShouldBeValid();

        [Theory]
        [InlineData("", "secret", "Dto.ClientId")]
        [InlineData("svc", "", "Dto.ClientSecret")]
        public void AuthenticateService_required_fields(string clientId, string secret, string prop)
            => new AuthenticateServiceCommandValidator()
                .Check(new AuthenticateServiceCommand(new ServiceTokenRequestDto(clientId, secret))).ShouldFailOn(prop);

        // ---------- Shared id-only delete/restore ----------

        [Fact]
        public void Delete_restore_id_only_validators_reject_non_positive_id()
        {
            new DeleteBusinessCommandValidator().Check(new DeleteBusinessCommand(TestIds.Of(0))).ShouldFailOn("Id");
            new RestoreBusinessCommandValidator().Check(new RestoreBusinessCommand(TestIds.Of(0))).ShouldFailOn("Id");
            new DeleteEmployeeCommandValidator().Check(new DeleteEmployeeCommand(TestIds.Of(0))).ShouldFailOn("Id");
            new RestoreEmployeeCommandValidator().Check(new RestoreEmployeeCommand(TestIds.Of(0))).ShouldFailOn("Id");
            new DeleteServiceCommandValidator().Check(new DeleteServiceCommand(TestIds.Of(0))).ShouldFailOn("Id");
            new RestoreServiceCommandValidator().Check(new RestoreServiceCommand(TestIds.Of(0))).ShouldFailOn("Id");
        }

        [Fact]
        public void Delete_restore_id_only_validators_accept_positive_id()
        {
            new DeleteBusinessCommandValidator().Check(new DeleteBusinessCommand(TestIds.Of(1))).ShouldBeValid();
            new RestoreServiceCommandValidator().Check(new RestoreServiceCommand(TestIds.Of(1))).ShouldBeValid();
        }

        // ---------- Employee time off (#271) ----------

        private static readonly DateTime TimeOffStart = new(2026, 6, 1, 10, 0, 0);

        private static CreateEmployeeTimeOffCommand ValidTimeOff() =>
            new(TestIds.Of(1), new CreateEmployeeTimeOffDto(TimeOffStart, TimeOffStart.AddHours(3)));

        [Fact]
        public void TimeOff_valid_passes()
            => new CreateEmployeeTimeOffCommandValidator().Check(ValidTimeOff()).ShouldBeValid();

        [Fact]
        public void TimeOff_end_not_after_start_fails()
            => new CreateEmployeeTimeOffCommandValidator()
                .Check(ValidTimeOff() with { Dto = new CreateEmployeeTimeOffDto(TimeOffStart, TimeOffStart) })
                .ShouldFailOn("Dto.End");

        // Wall-clock range (#290): the block is stored in `timestamp without time zone`
        // columns, so a zoned bound is rejected instead of being shifted or refused deeper.
        [Theory]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Local)]
        public void TimeOff_zoned_range_fails(DateTimeKind kind)
        {
            var result = new CreateEmployeeTimeOffCommandValidator().Check(ValidTimeOff() with
            {
                Dto = new CreateEmployeeTimeOffDto(
                    DateTime.SpecifyKind(TimeOffStart, kind),
                    DateTime.SpecifyKind(TimeOffStart.AddHours(3), kind))
            });

            result.ShouldFailOn("Dto.Start");
            result.ShouldFailOn("Dto.End");
        }

        // The delete had no validator at all, so an empty id reached the handler instead of
        // dying in the pipeline with a structured 400 like every other command (#312).
        [Fact]
        public void TimeOff_delete_valid_passes()
            => new DeleteEmployeeTimeOffCommandValidator()
                .Check(new DeleteEmployeeTimeOffCommand(TestIds.Of(1), TestIds.Of(2)))
                .ShouldBeValid();

        [Fact]
        public void TimeOff_delete_empty_employee_fails()
            => new DeleteEmployeeTimeOffCommandValidator()
                .Check(new DeleteEmployeeTimeOffCommand(Guid.Empty, TestIds.Of(2)))
                .ShouldFailOn("EmployeeId");

        [Fact]
        public void TimeOff_delete_empty_time_off_fails()
            => new DeleteEmployeeTimeOffCommandValidator()
                .Check(new DeleteEmployeeTimeOffCommand(TestIds.Of(1), Guid.Empty))
                .ShouldFailOn("TimeOffId");

        // ---------- Shared pagination validators ----------

        [Fact]
        public void Pagination_validators_reject_out_of_range()
        {
            new GetAllEmployeesQueryValidator().Check(new GetAllEmployeesQuery(0, 50)).ShouldFailOn("Page");
            new GetAuditLogsQueryValidator().Check(new GetAuditLogsQuery(null, null, null, null, null, 0, 50)).ShouldFailOn("Page");
        }
    }
}
