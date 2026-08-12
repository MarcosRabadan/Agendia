using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MRC.Agendia.Application.Appointments.DTO;
using MRC.Agendia.Application.Schedules.DTO;
using MRC.Agendia.Application.Services.DTO;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Infrastructure;

namespace MRC.Agendia.Tests.Integration.Infrastructure
{
    /// <summary>Uniform error body of the API, for asserting on error codes.</summary>
    public sealed record ApiError(string Code, string Message, string? TraceId);

    /// <summary>A provisioned, bookable business: owner + a full-week schedule for the
    /// year, a service, the owner's employee and a counter client.</summary>
    public sealed record BookableBusiness(ProvisionedOwner Owner, ServiceDto Service, int EmployeeId, int ClientId)
    {
        public int BusinessId => Owner.Business.Id;
        public string OwnerToken => Owner.Token;
    }

    /// <summary>
    /// Sets up a business whose calendar is open every weekday 09:00–18:00 for a given
    /// year, so appointment/availability flows have somewhere to land. Reused across the
    /// end-to-end appointment, availability and calendar tests.
    /// </summary>
    public static class BookableBusinessFactory
    {
        public static async Task<BookableBusiness> CreateAsync(HttpClient client,
                                                               IServiceProvider services,
                                                               string slug,
                                                               int year,
                                                               int durationMinutes = 30,
                                                               decimal price = 20m)
        {
            var owner = await TestProvisioning.ProvisionOwnerAsync(client, slug);
            await GenerateFullWeekScheduleAsync(client, owner, year);
            var service = await CreateServiceAsync(client, owner, "Servicio", durationMinutes, price);
            var clientId = SeedCounterClient(services);
            return new BookableBusiness(owner, service, owner.EmployeeId, clientId);
        }

        public static async Task GenerateFullWeekScheduleAsync(HttpClient client, ProvisionedOwner owner, int year)
        {
            var request = new GenerateScheduleRequestDto(
                BusinessId: owner.Business.Id,
                Year: year,
                Templates: new List<GenerateScheduleTemplateInputDto>
                {
                    new(
                        Name: "Base",
                        EffectiveFrom: new DateOnly(year, 1, 1),
                        EffectiveTo: new DateOnly(year, 12, 31),
                        IsDefault: true,
                        WeeklySlots: Enum.GetValues<DayOfWeek>()
                            .Select(d => new CreateWeeklyTimeSlotDto(d, new TimeOnly(9, 0), new TimeOnly(18, 0), TimeSlotType.Regular))
                            .ToList()),
                },
                IncludeNationalHolidays: false,
                IncludeLocalHolidays: false,
                VacationPeriods: null,
                CustomClosedDates: null);

            await SendAsync(client, HttpMethod.Post, $"/api/businesses/{owner.Business.Id}/schedules/generate", owner.Token, request);
        }

        public static async Task<ServiceDto> CreateServiceAsync(HttpClient client,
                                                                ProvisionedOwner owner,
                                                                string name,
                                                                int durationMinutes,
                                                                decimal price)
        {
            var response = await SendAsync(client, HttpMethod.Post, "/api/Service", owner.Token,
                new CreateServiceDto(owner.Business.Id, name, null, durationMinutes, price));
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<ServiceDto>();
            Assert.NotNull(created);
            return created!;
        }

        public static int SeedCounterClient(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgendiaDbContext>();
            var client = new Client { Name = "Cliente Mostrador", Phone = "600111222", Email = $"c-{Guid.NewGuid():N}@test.local" };
            db.Clients.Add(client);
            db.SaveChanges();
            return client.Id;
        }

        public static Task<HttpResponseMessage> PostAppointmentAsync(HttpClient client, string token, CreateAppointmentDto dto)
            => SendAsync(client, HttpMethod.Post, "/api/Appointment", token, dto);

        public static async Task<AppointmentDto> GetAppointmentAsync(HttpClient client, string token, int id)
        {
            var response = await SendAsync(client, HttpMethod.Get, $"/api/Appointment/{id}", token);
            response.EnsureSuccessStatusCode();
            var dto = await response.Content.ReadFromJsonAsync<AppointmentDto>();
            Assert.NotNull(dto);
            return dto!;
        }

        /// <summary>Sends a request with an explicit bearer token, leaving shared headers untouched.</summary>
        public static async Task<HttpResponseMessage> SendAsync(HttpClient client,
                                                                HttpMethod method,
                                                                string url,
                                                                string token,
                                                                object? body = null)
        {
            using var request = new HttpRequestMessage(method, url);
            if (body is not null)
                request.Content = JsonContent.Create(body);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await client.SendAsync(request);
        }
    }
}
