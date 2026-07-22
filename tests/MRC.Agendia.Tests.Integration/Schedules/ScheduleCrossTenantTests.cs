using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Schedules
{
    /// <summary>
    /// Defensive tests for issue #93. UpdateScheduleTemplate and
    /// UpdateScheduleOverride are already safe today because:
    ///   - The Update DTOs do not carry BusinessId.
    ///   - ScheduleService applies the update field by field, never via AutoMapper.
    ///   - The handlers validate via EnsureCanManageScheduleTemplateAsync /
    ///     EnsureCanManageScheduleOverrideAsync against the existing resource.
    /// These tests lock the behaviour: if someone ever adds BusinessId to the
    /// DTO or switches to AutoMapper without thinking, the tests blow up
    /// immediately, mirroring the takeover pattern fixed in #91 for Services.
    /// </summary>
    public class ScheduleCrossTenantTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public ScheduleCrossTenantTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task UpdateTemplate_OwnerB_TocaTemplateDeBusinessA_DevuelveForbidden()
        {
            var ownerA = await RegisterOwnerAsync("tpl-a");
            var ownerB = await RegisterOwnerAsync("tpl-b");

            var template = await CreateTemplateAsAsync(ownerA);

            var updateDto = new UpdateScheduleTemplateDto(
                Id: template.Id,
                Name: "Hijacked",
                EffectiveFrom: new DateOnly(2027, 1, 1),
                EffectiveTo: new DateOnly(2027, 12, 31),
                IsDefault: false,
                WeeklySlots: new List<CreateWeeklyTimeSlotDto>
                {
                    new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(13, 0), TimeSlotType.Regular),
                });

            // Note the URL targets Owner B's businessId but the templateId
            // belongs to Owner A. The controller only checks dto.Id ==
            // templateId, not the cross of businessId in the URL.
            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/businesses/{ownerB.Business.Id}/schedules/templates/{template.Id}")
            {
                Content = JsonContent.Create(updateDto)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerB.Token);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOverride_OwnerB_TocaOverrideDeBusinessA_DevuelveForbidden()
        {
            var ownerA = await RegisterOwnerAsync("ovr-a");
            var ownerB = await RegisterOwnerAsync("ovr-b");

            var scheduleOverride = await CreateOverrideAsAsync(ownerA);

            var updateDto = new UpdateScheduleOverrideDto(
                Id: scheduleOverride.Id,
                Date: scheduleOverride.Date,
                OverrideType: ScheduleOverrideType.Closed,
                Reason: "Hijacked",
                CustomSlots: null);

            using var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/businesses/{ownerB.Business.Id}/schedules/overrides/{scheduleOverride.Id}")
            {
                Content = JsonContent.Create(updateDto)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerB.Token);

            var response = await _client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ----- Helpers -----

        private async Task<ScheduleTemplateDto> CreateTemplateAsAsync(ProvisionedOwner owner)
        {
            var createDto = new CreateScheduleTemplateDto(
                BusinessId: owner.Business.Id,
                Name: "Template inicial",
                EffectiveFrom: new DateOnly(2026, 1, 1),
                EffectiveTo: new DateOnly(2026, 12, 31),
                IsDefault: true,
                WeeklySlots: new List<CreateWeeklyTimeSlotDto>
                {
                    new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(13, 0), TimeSlotType.Regular),
                });

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/businesses/{owner.Business.Id}/schedules/templates")
            {
                Content = JsonContent.Create(createDto)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<ScheduleTemplateDto>();
            Assert.NotNull(created);
            return created!;
        }

        private async Task<ScheduleOverrideDto> CreateOverrideAsAsync(ProvisionedOwner owner)
        {
            var createDto = new CreateScheduleOverrideDto(
                BusinessId: owner.Business.Id,
                Date: new DateOnly(2026, 8, 15),
                OverrideType: ScheduleOverrideType.Closed,
                Reason: "Cierre original",
                CustomSlots: null);

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/businesses/{owner.Business.Id}/schedules/overrides")
            {
                Content = JsonContent.Create(createDto)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);

            var response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<ScheduleOverrideDto>();
            Assert.NotNull(created);
            return created!;
        }

        // Every call provisions a brand new owner user id, so owner A and owner B
        // are distinct identities holding distinct tokens.
        private Task<ProvisionedOwner> RegisterOwnerAsync(string slug) =>
            TestProvisioning.ProvisionOwnerAsync(_client, slug);
    }
}
