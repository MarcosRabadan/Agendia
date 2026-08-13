using MRC.Agendia.Application.Auditing.Queries;
using MRC.Agendia.Application.Availability.Queries;
using MRC.Agendia.Application.Business.Commands.Delete;
using MRC.Agendia.Application.Business.Commands.Restore;
using MRC.Agendia.Application.Business.Queries.GetAll;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.DeviceTokens.Commands.Register;
using MRC.Agendia.Application.DeviceTokens.Commands.Remove;
using MRC.Agendia.Application.DeviceTokens.DTO;
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
using MRC.Agendia.Application.Services.Queries.GetAll;
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
    /// service-auth credentials, device tokens, and the shared id-only delete/restore
    /// and pagination validators of every aggregate.
    /// </summary>
    public class MiscValidatorsTests
    {
        // ---------- Availability ----------

        private static GetAvailabilityQuery ValidAvailability() =>
            new(BusinessId: 1, Date: new DateOnly(2026, 6, 1), ServiceId: 1, EmployeeId: null, StepMinutes: 30);

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
            => new GetAvailabilityQueryValidator().Check(ValidAvailability() with { EmployeeId = 0 }).ShouldFailOn("EmployeeId");

        [Fact]
        public void Availability_extra_service_equal_to_principal_fails()
            => new GetAvailabilityQueryValidator()
                .Check(ValidAvailability() with { ServiceId = 1, ExtraServiceIds = new[] { 1 } }).ShouldFailOn("");

        [Fact]
        public void Availability_valid_extras_pass()
            => new GetAvailabilityQueryValidator()
                .Check(ValidAvailability() with { ExtraServiceIds = new[] { 2, 3 } }).ShouldBeValid();

        // ---------- Waitlist ----------

        private static IClock ClockAt(DateTime now)
        {
            var clock = Substitute.For<IClock>();
            clock.BusinessNow.Returns(now);
            return clock;
        }

        private static JoinWaitlistDto ValidJoin() =>
            new(BusinessId: 1, ServiceId: 1, Date: new DateOnly(2026, 6, 1), StartTime: new TimeOnly(10, 0), EmployeeId: null);

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
                .Check(new JoinWaitlistCommand(ValidJoin() with { BusinessId = 0 })).ShouldFailOn("Dto.BusinessId");

        [Fact]
        public void LeaveWaitlist_entry_id_must_be_positive()
            => new LeaveWaitlistCommandValidator().Check(new LeaveWaitlistCommand(0)).ShouldFailOn("EntryId");

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
                .Check(new UpdateHolidayCommand(new UpdateHolidayCalendarDto(0, new DateOnly(2026, 5, 1), "X", HolidayScope.National, 2026)))
                .ShouldFailOn("Dto.Id");

        [Fact]
        public void DeleteHoliday_id_must_be_positive()
            => new DeleteHolidayCommandValidator().Check(new DeleteHolidayCommand(0)).ShouldFailOn("Id");

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

        // ---------- Device tokens ----------

        [Fact]
        public void RegisterDeviceToken_valid_passes()
            => new RegisterDeviceTokenCommandValidator()
                .Check(new RegisterDeviceTokenCommand(new RegisterDeviceTokenDto("tok", DevicePlatform.Android))).ShouldBeValid();

        [Fact]
        public void RegisterDeviceToken_empty_token_fails()
            => new RegisterDeviceTokenCommandValidator()
                .Check(new RegisterDeviceTokenCommand(new RegisterDeviceTokenDto("", DevicePlatform.Android))).ShouldFailOn("Dto.Token");

        [Fact]
        public void RegisterDeviceToken_platform_out_of_enum_fails()
            => new RegisterDeviceTokenCommandValidator()
                .Check(new RegisterDeviceTokenCommand(new RegisterDeviceTokenDto("tok", (DevicePlatform)99))).ShouldFailOn("Dto.Platform");

        [Fact]
        public void RemoveDeviceToken_empty_token_fails()
            => new RemoveDeviceTokenCommandValidator()
                .Check(new RemoveDeviceTokenCommand(new RemoveDeviceTokenDto(""))).ShouldFailOn("Dto.Token");

        // ---------- Shared id-only delete/restore ----------

        [Fact]
        public void Delete_restore_id_only_validators_reject_non_positive_id()
        {
            new DeleteBusinessCommandValidator().Check(new DeleteBusinessCommand(0)).ShouldFailOn("Id");
            new RestoreBusinessCommandValidator().Check(new RestoreBusinessCommand(0)).ShouldFailOn("Id");
            new DeleteEmployeeCommandValidator().Check(new DeleteEmployeeCommand(0)).ShouldFailOn("Id");
            new RestoreEmployeeCommandValidator().Check(new RestoreEmployeeCommand(0)).ShouldFailOn("Id");
            new DeleteServiceCommandValidator().Check(new DeleteServiceCommand(0)).ShouldFailOn("Id");
            new RestoreServiceCommandValidator().Check(new RestoreServiceCommand(0)).ShouldFailOn("Id");
        }

        [Fact]
        public void Delete_restore_id_only_validators_accept_positive_id()
        {
            new DeleteBusinessCommandValidator().Check(new DeleteBusinessCommand(1)).ShouldBeValid();
            new RestoreServiceCommandValidator().Check(new RestoreServiceCommand(1)).ShouldBeValid();
        }

        // ---------- Shared pagination validators ----------

        [Fact]
        public void Pagination_validators_reject_out_of_range()
        {
            new GetAllBusinessesQueryValidator().Check(new GetAllBusinessesQuery(0, 50)).ShouldFailOn("Page");
            new GetAllServicesQueryValidator().Check(new GetAllServicesQuery(1, 201)).ShouldFailOn("PageSize");
            new GetAllEmployeesQueryValidator().Check(new GetAllEmployeesQuery(0, 50)).ShouldFailOn("Page");
            new GetAuditLogsQueryValidator().Check(new GetAuditLogsQuery(null, null, null, null, null, 0, 50)).ShouldFailOn("Page");
        }
    }
}
