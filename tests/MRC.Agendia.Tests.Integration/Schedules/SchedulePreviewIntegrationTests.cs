using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MRC.Agendia.Application.Auth.DTO;
using MRC.Agendia.Application.Business.DTO;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Tests.Integration.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Schedules
{
    /// <summary>
    /// Coverage of the schedule preview endpoint (issue #54): same body as
    /// generate, returns the resulting calendar for the whole year and persists
    /// nothing.
    /// </summary>
    public class SchedulePreviewIntegrationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private const string OwnerPassword = "Owner1234!";
        private readonly HttpClient _client;

        public SchedulePreviewIntegrationTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Preview_DevuelveCalendarioAnual_YNoPersisteNada()
        {
            var owner = await RegisterOwnerAsync("preview");

            // 2027 is not a leap year: Mondays open 9-13, with one ad-hoc closed Monday.
            var request = new GenerateScheduleRequestDto(
                BusinessId: owner.Business.Id,
                Year: 2027,
                Templates: new List<GenerateScheduleTemplateInputDto>
                {
                    new(
                        Name: "Base",
                        EffectiveFrom: new DateOnly(2027, 1, 1),
                        EffectiveTo: new DateOnly(2027, 12, 31),
                        IsDefault: true,
                        WeeklySlots: new List<CreateWeeklyTimeSlotDto>
                        {
                            new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(13, 0), TimeSlotType.Regular),
                        }),
                },
                IncludeNationalHolidays: false,
                IncludeLocalHolidays: false,
                VacationPeriods: null,
                CustomClosedDates: new List<ClosedDateDto>
                {
                    new(new DateOnly(2027, 1, 11), "Cierre puntual"), // a Monday
                });

            var days = await PreviewAsync(owner, request);

            // Full year.
            Assert.Equal(365, days.Count);

            // A regular Monday is open with the configured slot.
            var openMonday = days.Single(d => d.Date == new DateOnly(2027, 1, 4));
            Assert.True(openMonday.IsOpen);
            Assert.NotNull(openMonday.TimeSlots);
            Assert.Contains(openMonday.TimeSlots!, ts => ts.StartTime == new TimeOnly(9, 0) && ts.EndTime == new TimeOnly(13, 0));

            // The ad-hoc closed Monday is closed.
            var closedMonday = days.Single(d => d.Date == new DateOnly(2027, 1, 11));
            Assert.False(closedMonday.IsOpen);

            // A Sunday is closed (non-working day).
            var sunday = days.Single(d => d.Date == new DateOnly(2027, 1, 3));
            Assert.False(sunday.IsOpen);

            // Preview persisted nothing: no templates were created.
            using var templatesRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/businesses/{owner.Business.Id}/schedules/templates");
            templatesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);
            var templatesResponse = await _client.SendAsync(templatesRequest);
            templatesResponse.EnsureSuccessStatusCode();
            var templates = await templatesResponse.Content.ReadFromJsonAsync<List<ScheduleTemplateDto>>();
            Assert.NotNull(templates);
            Assert.Empty(templates!);
        }

        [Fact]
        public async Task Preview_OwnerB_SobreBusinessA_DevuelveForbidden()
        {
            var ownerA = await RegisterOwnerAsync("prev-a");
            var ownerB = await RegisterOwnerAsync("prev-b");

            var request = new GenerateScheduleRequestDto(
                BusinessId: ownerA.Business.Id,
                Year: 2027,
                Templates: new List<GenerateScheduleTemplateInputDto>
                {
                    new(
                        Name: "Base",
                        EffectiveFrom: new DateOnly(2027, 1, 1),
                        EffectiveTo: new DateOnly(2027, 12, 31),
                        IsDefault: true,
                        WeeklySlots: new List<CreateWeeklyTimeSlotDto>
                        {
                            new(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(13, 0), TimeSlotType.Regular),
                        }),
                },
                IncludeNationalHolidays: false,
                IncludeLocalHolidays: false,
                VacationPeriods: null,
                CustomClosedDates: null);

            // Owner B tries to preview a schedule for Owner A's business.
            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/businesses/{ownerA.Business.Id}/schedules/preview")
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerB.Token);

            var response = await _client.SendAsync(httpRequest);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // ----- Helpers -----

        private async Task<List<CalendarDayDto>> PreviewAsync(RegisteredOwner owner, GenerateScheduleRequestDto request)
        {
            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"/api/businesses/{owner.Business.Id}/schedules/preview")
            {
                Content = JsonContent.Create(request)
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", owner.Token);

            var response = await _client.SendAsync(httpRequest);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var days = await response.Content.ReadFromJsonAsync<List<CalendarDayDto>>();
            Assert.NotNull(days);
            return days!;
        }

        private async Task<RegisteredOwner> RegisterOwnerAsync(string slug)
        {
            var unique = Guid.NewGuid().ToString("N");
            var email = $"{slug}-{unique}@test.local";
            var businessName = $"{slug}-{unique}";

            var registration = new RegisterOwnerDto(
                Email: email,
                Password: OwnerPassword,
                FullName: $"Owner {slug}",
                Phone: "600000000",
                BusinessName: businessName,
                BusinessAddress: "Calle Test 1",
                BusinessPhone: "910000000",
                BusinessEmail: $"info-{unique}@test.local",
                BusinessDescription: null);

            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register/owner", registration);
            registerResponse.EnsureSuccessStatusCode();
            var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
            Assert.NotNull(auth);

            var businessesResponse = await _client.GetAsync("/api/Business?page=1&pageSize=200");
            businessesResponse.EnsureSuccessStatusCode();
            var paged = await businessesResponse.Content.ReadFromJsonAsync<PagedResult<BusinessPublicDto>>();
            Assert.NotNull(paged);
            var business = paged!.Items.First(b => b.Name == businessName);

            return new RegisteredOwner(auth!.AccessToken, business);
        }

        private sealed record RegisteredOwner(string Token, BusinessPublicDto Business);
    }
}
