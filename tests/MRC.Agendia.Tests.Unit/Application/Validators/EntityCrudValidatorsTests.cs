using MRC.Agendia.Application.Business.Commands.Create;
using MRC.Agendia.Application.Business.Commands.Update;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Employees.Commands.Create;
using MRC.Agendia.Application.Employees.Commands.Update;
using MRC.Agendia.Application.Employees.DTO;
using MRC.Agendia.Application.Services.Commands.Create;
using MRC.Agendia.Application.Services.Commands.Update;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Enums;

namespace MRC.Agendia.Tests.Unit.Application.Validators
{
    /// <summary>
    /// Exhaustive rule coverage for the Business/Service/Employee/Client CRUD command
    /// validators: the happy path passes, and every constraint (required, length,
    /// email, ranges, enum/language whitelist) fails on its own property.
    /// </summary>
    public class EntityCrudValidatorsTests
    {
        // ---------- Business ----------

        private static CreateBusinessDto ValidBusiness() =>
            new("owner-1");

        [Fact]
        public void CreateBusiness_valid_passes()
            => new CreateBusinessCommandValidator().Check(new CreateBusinessCommand(ValidBusiness())).ShouldBeValid();

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void CreateBusiness_owner_required(string? owner)
            => new CreateBusinessCommandValidator()
                .Check(new CreateBusinessCommand(ValidBusiness() with { OwnerUserId = owner! })).ShouldFailOn("Dto.OwnerUserId");

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(8761)]
        public void CreateBusiness_cancellation_window_out_of_range_fails(int hours)
            => new CreateBusinessCommandValidator()
                .Check(new CreateBusinessCommand(ValidBusiness() with { CancellationWindowHours = hours }))
                .ShouldFailOn("Dto.CancellationWindowHours");

        [Theory]
        [InlineData(1)]
        [InlineData(8760)]
        [InlineData(null)]
        public void CreateBusiness_cancellation_window_valid_passes(int? hours)
            => new CreateBusinessCommandValidator()
                .Check(new CreateBusinessCommand(ValidBusiness() with { CancellationWindowHours = hours })).ShouldBeValid();

        [Theory]
        [InlineData("de")]
        [InlineData("xx")]
        [InlineData("")]
        public void CreateBusiness_unsupported_language_fails(string lang)
            => new CreateBusinessCommandValidator()
                .Check(new CreateBusinessCommand(ValidBusiness() with { DefaultLanguage = lang })).ShouldFailOn("Dto.DefaultLanguage");

        [Theory]
        [InlineData("es")]
        [InlineData("en")]
        [InlineData("fr")]
        public void CreateBusiness_supported_language_passes(string lang)
            => new CreateBusinessCommandValidator()
                .Check(new CreateBusinessCommand(ValidBusiness() with { DefaultLanguage = lang })).ShouldBeValid();

        [Theory]
        [InlineData(AppointmentStatus.Cancelled)]
        [InlineData(AppointmentStatus.Completed)]
        [InlineData(AppointmentStatus.NoShow)]
        public void CreateBusiness_non_initial_default_status_fails(AppointmentStatus status)
            => new CreateBusinessCommandValidator()
                .Check(new CreateBusinessCommand(ValidBusiness() with { DefaultAppointmentStatus = status }))
                .ShouldFailOn("Dto.DefaultAppointmentStatus");

        [Theory]
        [InlineData(AppointmentStatus.Pending)]
        [InlineData(AppointmentStatus.Confirmed)]
        public void CreateBusiness_initial_default_status_passes(AppointmentStatus status)
            => new CreateBusinessCommandValidator()
                .Check(new CreateBusinessCommand(ValidBusiness() with { DefaultAppointmentStatus = status })).ShouldBeValid();

        private static UpdateBusinessDto ValidBusinessUpdate() => new(5, IsActive: true);

        [Fact]
        public void UpdateBusiness_valid_passes()
            => new UpdateBusinessCommandValidator().Check(new UpdateBusinessCommand(ValidBusinessUpdate())).ShouldBeValid();

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public void UpdateBusiness_id_must_be_positive(int id)
            => new UpdateBusinessCommandValidator()
                .Check(new UpdateBusinessCommand(ValidBusinessUpdate() with { Id = id })).ShouldFailOn("Dto.Id");

        // ---------- Service ----------

        private static CreateServiceDto ValidService() => new(7, 30);

        [Fact]
        public void CreateService_valid_passes()
            => new CreateServiceCommandValidator().Check(new CreateServiceCommand(ValidService())).ShouldBeValid();

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void CreateService_business_id_must_be_positive(int businessId)
            => new CreateServiceCommandValidator()
                .Check(new CreateServiceCommand(ValidService() with { BusinessId = businessId })).ShouldFailOn("Dto.BusinessId");

        [Theory]
        [InlineData(0)]
        [InlineData(-30)]
        [InlineData(24 * 60 + 1)]
        public void CreateService_duration_out_of_range_fails(int minutes)
            => new CreateServiceCommandValidator()
                .Check(new CreateServiceCommand(ValidService() with { DurationMinutes = minutes })).ShouldFailOn("Dto.DurationMinutes");

        [Theory]
        [InlineData(1)]
        [InlineData(24 * 60)]
        public void CreateService_duration_boundaries_pass(int minutes)
            => new CreateServiceCommandValidator()
                .Check(new CreateServiceCommand(ValidService() with { DurationMinutes = minutes })).ShouldBeValid();

        [Fact]
        public void UpdateService_valid_passes()
            => new UpdateServiceCommandValidator().Check(new UpdateServiceCommand(new UpdateServiceDto(1, 30))).ShouldBeValid();

        [Fact]
        public void UpdateService_id_must_be_positive()
            => new UpdateServiceCommandValidator()
                .Check(new UpdateServiceCommand(new UpdateServiceDto(0, 30))).ShouldFailOn("Dto.Id");

        // ---------- Employee ----------

        private static CreateEmployeeDto ValidEmployee() => new(7);

        [Fact]
        public void CreateEmployee_valid_passes()
            => new CreateEmployeeCommandValidator().Check(new CreateEmployeeCommand(ValidEmployee())).ShouldBeValid();

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public void CreateEmployee_capacity_out_of_range_fails(int cap)
            => new CreateEmployeeCommandValidator()
                .Check(new CreateEmployeeCommand(ValidEmployee() with { MaxConcurrentAppointments = cap }))
                .ShouldFailOn("Dto.MaxConcurrentAppointments");

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        public void CreateEmployee_capacity_boundaries_pass(int cap)
            => new CreateEmployeeCommandValidator()
                .Check(new CreateEmployeeCommand(ValidEmployee() with { MaxConcurrentAppointments = cap })).ShouldBeValid();

        [Fact]
        public void CreateEmployee_business_id_must_be_positive()
            => new CreateEmployeeCommandValidator()
                .Check(new CreateEmployeeCommand(ValidEmployee() with { BusinessId = 0 })).ShouldFailOn("Dto.BusinessId");

        [Fact]
        public void UpdateEmployee_valid_passes()
            => new UpdateEmployeeCommandValidator()
                .Check(new UpdateEmployeeCommand(new UpdateEmployeeDto(1, IsActive: true, MaxConcurrentAppointments: 2))).ShouldBeValid();

        [Theory]
        [InlineData(0)]
        [InlineData(101)]
        public void UpdateEmployee_capacity_out_of_range_fails(int cap)
            => new UpdateEmployeeCommandValidator()
                .Check(new UpdateEmployeeCommand(new UpdateEmployeeDto(1, IsActive: true, MaxConcurrentAppointments: cap)))
                .ShouldFailOn("Dto.MaxConcurrentAppointments");
    }
}
