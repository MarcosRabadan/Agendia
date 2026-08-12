using MRC.Agendia.Application.Business.Commands.Create;
using MRC.Agendia.Application.Business.Commands.Update;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Clients.Commands.Create;
using MRC.Agendia.Application.Clients.Commands.Update;
using MRC.Agendia.Application.Clients.DTO;
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
        private static string Str(int n) => new('a', n);

        // ---------- Business ----------

        private static CreateBusinessDto ValidBusiness() =>
            new("Peluqueria Ana", "desc", "Calle Mayor 1", "600100200", "info@ana.com", "owner-1");

        [Fact]
        public void CreateBusiness_valid_passes()
            => new CreateBusinessCommandValidator().Check(new CreateBusinessCommand(ValidBusiness())).ShouldBeValid();

        [Theory]
        [InlineData("", "Dto.Name")]
        [InlineData(null, "Dto.Name")]
        public void CreateBusiness_name_required(string? name, string prop)
            => new CreateBusinessCommandValidator()
                .Check(new CreateBusinessCommand(ValidBusiness() with { Name = name! })).ShouldFailOn(prop);

        [Fact]
        public void CreateBusiness_name_too_long_fails()
            => new CreateBusinessCommandValidator()
                .Check(new CreateBusinessCommand(ValidBusiness() with { Name = Str(201) })).ShouldFailOn("Dto.Name");

        [Theory]
        [InlineData("")]
        [InlineData("not-an-email")]
        public void CreateBusiness_email_must_be_valid(string email)
            => new CreateBusinessCommandValidator()
                .Check(new CreateBusinessCommand(ValidBusiness() with { Email = email })).ShouldFailOn("Dto.Email");

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

        private static UpdateBusinessDto ValidBusinessUpdate() =>
            new(5, "Peluqueria Ana", "desc", "Calle Mayor 1", "600100200", "info@ana.com", true);

        [Fact]
        public void UpdateBusiness_valid_passes()
            => new UpdateBusinessCommandValidator().Check(new UpdateBusinessCommand(ValidBusinessUpdate())).ShouldBeValid();

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public void UpdateBusiness_id_must_be_positive(int id)
            => new UpdateBusinessCommandValidator()
                .Check(new UpdateBusinessCommand(ValidBusinessUpdate() with { Id = id })).ShouldFailOn("Dto.Id");

        [Fact]
        public void UpdateBusiness_invalid_email_fails()
            => new UpdateBusinessCommandValidator()
                .Check(new UpdateBusinessCommand(ValidBusinessUpdate() with { Email = "bad" })).ShouldFailOn("Dto.Email");

        // ---------- Service ----------

        private static CreateServiceDto ValidService() => new(7, "Corte", "desc", 30, 15m);

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
        public void CreateService_negative_price_fails()
            => new CreateServiceCommandValidator()
                .Check(new CreateServiceCommand(ValidService() with { Price = -0.01m })).ShouldFailOn("Dto.Price");

        [Fact]
        public void CreateService_zero_price_passes()
            => new CreateServiceCommandValidator()
                .Check(new CreateServiceCommand(ValidService() with { Price = 0m })).ShouldBeValid();

        [Fact]
        public void UpdateService_valid_passes()
            => new UpdateServiceCommandValidator().Check(new UpdateServiceCommand(new UpdateServiceDto(1, "Corte", null, 30, 10m))).ShouldBeValid();

        [Fact]
        public void UpdateService_id_must_be_positive()
            => new UpdateServiceCommandValidator()
                .Check(new UpdateServiceCommand(new UpdateServiceDto(0, "Corte", null, 30, 10m))).ShouldFailOn("Dto.Id");

        // ---------- Employee ----------

        private static CreateEmployeeDto ValidEmployee() => new(7, "Ana Perez", "ana@x.com", "600", null, 1);

        [Fact]
        public void CreateEmployee_valid_passes()
            => new CreateEmployeeCommandValidator().Check(new CreateEmployeeCommand(ValidEmployee())).ShouldBeValid();

        [Fact]
        public void CreateEmployee_null_email_passes()
            => new CreateEmployeeCommandValidator()
                .Check(new CreateEmployeeCommand(ValidEmployee() with { Email = null })).ShouldBeValid();

        [Fact]
        public void CreateEmployee_invalid_email_fails()
            => new CreateEmployeeCommandValidator()
                .Check(new CreateEmployeeCommand(ValidEmployee() with { Email = "nope" })).ShouldFailOn("Dto.Email");

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
                .Check(new UpdateEmployeeCommand(new UpdateEmployeeDto(1, "Ana", null, null, true, 2))).ShouldBeValid();

        [Fact]
        public void UpdateEmployee_empty_name_fails()
            => new UpdateEmployeeCommandValidator()
                .Check(new UpdateEmployeeCommand(new UpdateEmployeeDto(1, "", null, null, true, 2))).ShouldFailOn("Dto.FullName");

        // ---------- Client ----------

        private static CreateClientDto ValidClient() => new("Luis", "600999888", "luis@x.com");

        [Fact]
        public void CreateClient_valid_passes()
            => new CreateClientCommandValidator().Check(new CreateClientCommand(ValidClient())).ShouldBeValid();

        [Fact]
        public void CreateClient_null_email_passes()
            => new CreateClientCommandValidator()
                .Check(new CreateClientCommand(ValidClient() with { Email = null })).ShouldBeValid();

        [Theory]
        [InlineData("", "Dto.Name")]
        [InlineData(null, "Dto.Name")]
        public void CreateClient_name_required(string? name, string prop)
            => new CreateClientCommandValidator()
                .Check(new CreateClientCommand(ValidClient() with { Name = name! })).ShouldFailOn(prop);

        [Fact]
        public void CreateClient_empty_phone_fails()
            => new CreateClientCommandValidator()
                .Check(new CreateClientCommand(ValidClient() with { Phone = "" })).ShouldFailOn("Dto.Phone");

        [Fact]
        public void CreateClient_invalid_email_fails()
            => new CreateClientCommandValidator()
                .Check(new CreateClientCommand(ValidClient() with { Email = "bad" })).ShouldFailOn("Dto.Email");

        [Fact]
        public void CreateBusinessClient_valid_passes()
            => new CreateBusinessClientCommandValidator()
                .Check(new CreateBusinessClientCommand(7, ValidClient())).ShouldBeValid();

        [Fact]
        public void CreateBusinessClient_business_id_must_be_positive()
            => new CreateBusinessClientCommandValidator()
                .Check(new CreateBusinessClientCommand(0, ValidClient())).ShouldFailOn("BusinessId");

        [Fact]
        public void UpdateClient_valid_passes()
            => new UpdateClientCommandValidator()
                .Check(new UpdateClientCommand(new UpdateClientDto(1, "Luis", "600", "luis@x.com"))).ShouldBeValid();

        [Fact]
        public void UpdateClient_id_must_be_positive()
            => new UpdateClientCommandValidator()
                .Check(new UpdateClientCommand(new UpdateClientDto(0, "Luis", "600", null))).ShouldFailOn("Dto.Id");
    }
}
