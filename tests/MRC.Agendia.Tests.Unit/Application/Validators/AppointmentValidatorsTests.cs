using MRC.Agendia.Application.Appointments.Commands.Crud;
using MRC.Agendia.Application.Appointments.Commands.Delay;
using MRC.Agendia.Application.Appointments.Commands.Series;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Appointments.Queries.ByDateRange;
using MRC.Agendia.Application.Appointments.Queries.GetAll;
using MRC.Agendia.Application.Appointments.Queries.MyAppointments;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Tests.Unit.Application.Validators
{
    /// <summary>
    /// Rule coverage for the appointment command/query validators: ids, date order,
    /// initial-status whitelist, extra-services constraints, series identifiers,
    /// delay bounds, date-range caps and pagination.
    /// </summary>
    public class AppointmentValidatorsTests
    {
        private static readonly DateTime Start = new(2026, 6, 1, 10, 0, 0);
        private static readonly DateTime End = new(2026, 6, 1, 10, 30, 0);

        private static CreateAppointmentDto ValidCreate() => new("client-1", 1, 1, Start, End, null);

        // ---------- Create ----------

        [Fact]
        public void Create_valid_passes()
            => new CreateAppointmentCommandValidator().Check(new CreateAppointmentCommand(ValidCreate())).ShouldBeValid();

        [Fact]
        public void Create_empty_client_user_id_fails()
            => new CreateAppointmentCommandValidator()
                .Check(new CreateAppointmentCommand(ValidCreate() with { ClientUserId = "" })).ShouldFailOn("Dto.ClientUserId");

        [Theory]
        [InlineData(0, 1, "Dto.EmployeeId")]
        [InlineData(1, 0, "Dto.ServiceId")]
        public void Create_ids_must_be_positive(int emp, int svc, string prop)
            => new CreateAppointmentCommandValidator()
                .Check(new CreateAppointmentCommand(ValidCreate() with { EmployeeId = emp, ServiceId = svc }))
                .ShouldFailOn(prop);

        [Fact]
        public void Create_end_not_after_start_fails()
            => new CreateAppointmentCommandValidator()
                .Check(new CreateAppointmentCommand(ValidCreate() with { EndDate = Start })).ShouldFailOn("Dto.EndDate");

        [Fact]
        public void Create_missing_start_fails()
            => new CreateAppointmentCommandValidator()
                .Check(new CreateAppointmentCommand(ValidCreate() with { StartDate = default })).ShouldFailOn("Dto.StartDate");

        [Theory]
        [InlineData(AppointmentStatus.Cancelled)]
        [InlineData(AppointmentStatus.Completed)]
        [InlineData(AppointmentStatus.NoShow)]
        public void Create_non_initial_status_fails(AppointmentStatus status)
            => new CreateAppointmentCommandValidator()
                .Check(new CreateAppointmentCommand(ValidCreate() with { Status = status })).ShouldFailOn("Dto.Status");

        [Theory]
        [InlineData(AppointmentStatus.Pending)]
        [InlineData(AppointmentStatus.Confirmed)]
        public void Create_initial_status_passes(AppointmentStatus status)
            => new CreateAppointmentCommandValidator()
                .Check(new CreateAppointmentCommand(ValidCreate() with { Status = status })).ShouldBeValid();

        [Fact]
        public void Create_null_status_passes()
            => new CreateAppointmentCommandValidator()
                .Check(new CreateAppointmentCommand(ValidCreate() with { Status = null })).ShouldBeValid();

        [Fact]
        public void Create_valid_extra_services_pass()
            => new CreateAppointmentCommandValidator()
                .Check(new CreateAppointmentCommand(ValidCreate() with { ExtraServiceIds = new[] { 2, 3 } })).ShouldBeValid();

        [Fact]
        public void Create_extra_service_duplicate_fails()
            => new CreateAppointmentCommandValidator()
                .Check(new CreateAppointmentCommand(ValidCreate() with { ExtraServiceIds = new[] { 2, 2 } })).ShouldFailOn("Dto");

        [Fact]
        public void Create_extra_service_equal_to_principal_fails()
            => new CreateAppointmentCommandValidator()
                .Check(new CreateAppointmentCommand(ValidCreate() with { ServiceId = 1, ExtraServiceIds = new[] { 1 } })).ShouldFailOn("Dto");

        [Fact]
        public void Create_too_many_extra_services_fails()
            => new CreateAppointmentCommandValidator()
                .Check(new CreateAppointmentCommand(ValidCreate() with { ExtraServiceIds = Enumerable.Range(2, 11).ToArray() }))
                .ShouldFailOn("Dto.ExtraServiceIds.Count");

        // ---------- Update ----------

        private static UpdateAppointmentDto ValidUpdate() => new(1, "client-1", 1, 1, Start, End, AppointmentStatus.Confirmed, null);

        [Fact]
        public void Update_valid_passes()
            => new UpdateAppointmentCommandValidator().Check(new UpdateAppointmentCommand(ValidUpdate())).ShouldBeValid();

        [Fact]
        public void Update_id_must_be_positive()
            => new UpdateAppointmentCommandValidator()
                .Check(new UpdateAppointmentCommand(ValidUpdate() with { Id = 0 })).ShouldFailOn("Dto.Id");

        [Fact]
        public void Update_status_out_of_enum_fails()
            => new UpdateAppointmentCommandValidator()
                .Check(new UpdateAppointmentCommand(ValidUpdate() with { Status = (AppointmentStatus)99 })).ShouldFailOn("Dto.Status");

        [Fact]
        public void Update_end_not_after_start_fails()
            => new UpdateAppointmentCommandValidator()
                .Check(new UpdateAppointmentCommand(ValidUpdate() with { EndDate = Start.AddMinutes(-1) })).ShouldFailOn("Dto.EndDate");

        // ---------- Delete / Restore ----------

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Delete_id_must_be_positive(int id)
            => new DeleteAppointmentCommandValidator().Check(new DeleteAppointmentCommand(id)).ShouldFailOn("Id");

        [Fact]
        public void Restore_id_must_be_positive()
            => new RestoreAppointmentCommandValidator().Check(new RestoreAppointmentCommand(0)).ShouldFailOn("Id");

        // ---------- Notify delay ----------

        private static NotifyDelayDto ValidDelay() => new(EmployeeId: null, DelayMinutes: 15, MaxAppointments: null);

        [Fact]
        public void NotifyDelay_valid_passes()
            => new NotifyDelayCommandValidator().Check(new NotifyDelayCommand(1, ValidDelay())).ShouldBeValid();

        [Theory]
        [InlineData(0)]
        [InlineData(601)]
        public void NotifyDelay_minutes_out_of_range_fails(int minutes)
            => new NotifyDelayCommandValidator()
                .Check(new NotifyDelayCommand(1, ValidDelay() with { DelayMinutes = minutes })).ShouldFailOn("Dto.DelayMinutes");

        [Fact]
        public void NotifyDelay_business_id_must_be_positive()
            => new NotifyDelayCommandValidator().Check(new NotifyDelayCommand(0, ValidDelay())).ShouldFailOn("BusinessId");

        [Fact]
        public void NotifyDelay_employee_id_when_present_must_be_positive()
            => new NotifyDelayCommandValidator()
                .Check(new NotifyDelayCommand(1, ValidDelay() with { EmployeeId = 0 })).ShouldFailOn("Dto.EmployeeId");

        [Fact]
        public void NotifyDelay_max_appointments_when_present_must_be_positive()
            => new NotifyDelayCommandValidator()
                .Check(new NotifyDelayCommand(1, ValidDelay() with { MaxAppointments = 0 })).ShouldFailOn("Dto.MaxAppointments");

        // ---------- Series (cancel / delete / move) ----------

        [Fact]
        public void CancelSeries_empty_id_fails()
            => new CancelAppointmentSeriesCommandValidator().Check(new CancelAppointmentSeriesCommand(Guid.Empty)).ShouldFailOn("SeriesId");

        [Fact]
        public void DeleteSeries_empty_id_fails()
            => new DeleteAppointmentSeriesCommandValidator().Check(new DeleteAppointmentSeriesCommand(Guid.Empty)).ShouldFailOn("SeriesId");

        [Fact]
        public void MoveSeries_valid_passes()
            => new MoveAppointmentSeriesCommandValidator()
                .Check(new MoveAppointmentSeriesCommand(Guid.NewGuid(), new MoveAppointmentSeriesDto(null, 7))).ShouldBeValid();

        [Fact]
        public void MoveSeries_no_change_fails()
            => new MoveAppointmentSeriesCommandValidator()
                .Check(new MoveAppointmentSeriesCommand(Guid.NewGuid(), new MoveAppointmentSeriesDto(null, 0))).ShouldFailOn("Dto");

        [Theory]
        [InlineData(367)]
        [InlineData(-367)]
        public void MoveSeries_day_shift_out_of_range_fails(int shift)
            => new MoveAppointmentSeriesCommandValidator()
                .Check(new MoveAppointmentSeriesCommand(Guid.NewGuid(), new MoveAppointmentSeriesDto(null, shift))).ShouldFailOn("Dto.DayShift");

        [Fact]
        public void MoveSeries_new_time_with_zero_shift_passes()
            => new MoveAppointmentSeriesCommandValidator()
                .Check(new MoveAppointmentSeriesCommand(Guid.NewGuid(), new MoveAppointmentSeriesDto(new TimeOnly(17, 0), 0))).ShouldBeValid();

        // ---------- Queries ----------

        [Fact]
        public void ByDateRange_valid_passes()
            => new GetAppointmentsByDateRangeQueryValidator()
                .Check(new GetAppointmentsByDateRangeQuery(1, Start, Start.AddDays(30))).ShouldBeValid();

        [Fact]
        public void ByDateRange_end_before_start_fails()
            => new GetAppointmentsByDateRangeQueryValidator()
                .Check(new GetAppointmentsByDateRangeQuery(1, Start, Start.AddDays(-1))).ShouldFailOn("EndDate");

        [Fact]
        public void ByDateRange_over_366_days_fails()
            => new GetAppointmentsByDateRangeQueryValidator()
                .Check(new GetAppointmentsByDateRangeQuery(1, Start, Start.AddDays(400))).ShouldFailOn("");

        [Theory]
        [InlineData(0, 50, "Page")]
        [InlineData(1, 0, "PageSize")]
        [InlineData(1, 201, "PageSize")]
        public void GetAll_pagination_bounds(int page, int pageSize, string prop)
            => new GetAllAppointmentsQueryValidator().Check(new GetAllAppointmentsQuery(page, pageSize)).ShouldFailOn(prop);

        [Fact]
        public void GetAll_valid_pagination_passes()
            => new GetAllAppointmentsQueryValidator().Check(new GetAllAppointmentsQuery(1, 200)).ShouldBeValid();

        [Fact]
        public void GetMyAppointments_pagination_bounds()
            => new GetMyAppointmentsAsClientQueryValidator().Check(new GetMyAppointmentsAsClientQuery(0, 50)).ShouldFailOn("Page");
    }
}
