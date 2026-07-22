using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Clients.DTO;
using MRC.Agendia.Application.DeviceTokens.DTO;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Constants;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Notifications
{
    /// <summary>
    /// End-to-end coverage for push device tokens (#51): a client registers a token,
    /// then a booking confirmation fans out a push to it; register/remove round-trips
    /// to the DB; and the endpoint requires authentication.
    ///
    /// The device token is keyed by the caller's user id, and the client books for
    /// themselves, so the forged token must carry the same user id stored in
    /// Client.UserId.
    /// </summary>
    public class DeviceTokenIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const int Year = 2035;
        private static readonly DateOnly SlotDate = new(Year, 6, 4);
        private static readonly TimeOnly SlotTime = new(10, 0);

        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public DeviceTokenIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ReservarTrasRegistrarToken_EnviaPushAlCliente()
        {
            var owner = await RegisterOwnerAsync("push-ok");
            await GenerateScheduleAsync(owner);
            var service = await CreateServiceAsync(owner, "Corte", 30, 20m);
            var (clientToken, clientId) = await CreateClientAccountAsync("push-c");

            var deviceToken = $"tok-{Guid.NewGuid():N}";
            var reg = await RegisterDeviceTokenAsync(clientToken, deviceToken, DevicePlatform.Android);
            Assert.Equal(HttpStatusCode.NoContent, reg.StatusCode);

            // The client books their own appointment -> confirmation -> push fan-out.
            var start = SlotDate.ToDateTime(SlotTime);
            var book = await BookAsClientAsync(clientToken, clientId, owner.EmployeeId, service.Id, start);
            Assert.Equal(HttpStatusCode.Created, book.StatusCode);

            var push = await _factory.PushSender.WaitForTokenAsync(deviceToken);
            Assert.NotNull(push);
            Assert.Contains("confirmada", push!.Title, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task RegistrarYDarDeBajaToken_ActualizaLaBaseDeDatos()
        {
            var (clientToken, _) = await CreateClientAccountAsync("push-rm");
            var deviceToken = $"tok-{Guid.NewGuid():N}";

            (await RegisterDeviceTokenAsync(clientToken, deviceToken, DevicePlatform.Ios)).EnsureSuccessStatusCode();
            Assert.True(await TokenExistsAsync(deviceToken));

            (await RemoveDeviceTokenAsync(clientToken, deviceToken)).EnsureSuccessStatusCode();
            Assert.False(await TokenExistsAsync(deviceToken));
        }

        [Fact]
        public async Task RegistrarToken_SinAutenticar_DevuelveUnauthorized()
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/notifications/device-tokens")
            {
                Content = JsonContent.Create(new RegisterDeviceTokenDto("tok-anon", DevicePlatform.Web))
            };
            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        // ----- Helpers -----

        private async Task<HttpResponseMessage> RegisterDeviceTokenAsync(string token, string deviceToken, DevicePlatform platform)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/notifications/device-tokens")
            {
                Content = JsonContent.Create(new RegisterDeviceTokenDto(deviceToken, platform))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await _client.SendAsync(request);
        }

        private async Task<HttpResponseMessage> RemoveDeviceTokenAsync(string token, string deviceToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/notifications/device-tokens")
            {
                Content = JsonContent.Create(new RemoveDeviceTokenDto(deviceToken))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await _client.SendAsync(request);
        }

        private async Task<bool> TokenExistsAsync(string deviceToken)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            return await db.DeviceTokens.AnyAsync(d => d.Token == deviceToken);
        }

        private async Task<HttpResponseMessage> BookAsClientAsync(string clientToken, int clientId, int employeeId, int serviceId, DateTime start)
        {
            var dto = new CreateAppointmentDto(clientId, employeeId, serviceId, start, start.AddMinutes(30), Notes: null);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Appointment") { Content = JsonContent.Create(dto) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", clientToken);
            return await _client.SendAsync(request);
        }

        /// <summary>
        /// Creates a client row bound to a Harmony user id and returns a token minted
        /// for that same user id, so the client can act on their own records.
        /// </summary>
        private async Task<(string Token, int ClientId)> CreateClientAccountAsync(string slug)
        {
            var unique = Guid.NewGuid().ToString("N");
            var clientUserId = $"harmony-client-{unique}";
            var adminToken = TestTokenFactory.Create($"admin-{unique}", Roles.Admin);

            var created = await TestProvisioning.PostAsync<CreateClientDto, ClientDto>(
                _client,
                "/api/Client",
                new CreateClientDto($"Cliente {slug}", "600999888", $"{slug}-{unique}@test.local", clientUserId),
                adminToken);

            return (TestTokenFactory.Create(clientUserId, Roles.Client), created.Id);
        }

        private async Task GenerateScheduleAsync(ProvisionedOwner owner)
        {
            var request = new GenerateScheduleRequestDto(
                BusinessId: owner.Business.Id,
                Year: Year,
                Templates: new List<GenerateScheduleTemplateInputDto>
                {
                    new(
                        Name: "Base",
                        EffectiveFrom: new DateOnly(Year, 1, 1),
                        EffectiveTo: new DateOnly(Year, 12, 31),
                        IsDefault: true,
                        WeeklySlots: Enum.GetValues<DayOfWeek>()
                            .Select(d => new CreateWeeklyTimeSlotDto(d, new TimeOnly(9, 0), new TimeOnly(18, 0), TimeSlotType.Regular))
                            .ToList()),
                },
                IncludeNationalHolidays: false,
                IncludeLocalHolidays: false,
                VacationPeriods: null,
                CustomClosedDates: null);

            using var gen = new HttpRequestMessage(HttpMethod.Post, $"/api/businesses/{owner.Business.Id}/schedules/generate")
            {
                Content = JsonContent.Create(request)
            };
            gen.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
            (await _client.SendAsync(gen)).EnsureSuccessStatusCode();
        }

        private async Task<ServiceDto> CreateServiceAsync(ProvisionedOwner owner, string name, int durationMinutes, decimal price)
        {
            var dto = new CreateServiceDto(owner.Business.Id, name, null, durationMinutes, price);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/Service") { Content = JsonContent.Create(dto) };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<ServiceDto>();
            Assert.NotNull(created);
            return created!;
        }

        private Task<ProvisionedOwner> RegisterOwnerAsync(string slug) =>
            TestProvisioning.ProvisionOwnerAsync(_client, slug);
    }
}
