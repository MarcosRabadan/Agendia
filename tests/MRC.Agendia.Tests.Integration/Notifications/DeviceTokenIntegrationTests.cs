using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.DeviceTokens.DTO;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Notifications
{
    /// <summary>
    /// End-to-end coverage for push device tokens (#51): register/remove round-trips to
    /// the DB, and the endpoint requires authentication. The token is keyed by the
    /// caller's user id (the JWT "sub"). Push delivery on booking is not covered here:
    /// notification delivery is a no-op until it moves to events (#246).
    /// </summary>
    public class DeviceTokenIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public DeviceTokenIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task RegistrarYDarDeBajaToken_ActualizaLaBaseDeDatos()
        {
            var clientToken = TestProvisioning.ProvisionClient("push-rm").Token;
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
    }
}
