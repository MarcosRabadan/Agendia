using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Infrastructure.Repositories;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Repositories
{
    public class AppointmentRepositoryTests
    {
        private static AgendiaDbContext NewContext(string dbName) =>
            new(new DbContextOptionsBuilder<AgendiaDbContext>()
                .UseInMemoryDatabase(dbName)
                .ConfigureWarnings(w => w.Ignore(
                    CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
                .Options);

        [Fact]
        public async Task GetByIdWithDetailsAsync_CargaPadresAunqueEstenSoftDeleted()
        {
            var dbName = $"appt-repo-{Guid.NewGuid()}";
            int appointmentId;

            using (var ctx = NewContext(dbName))
            {
                var business = new Business { Name = "B", Address = "x", Phone = "1", Email = "b@x.com", IsActive = true };
                var client = new Client { Name = "Ana", Phone = "600", Email = "ana@x.com" };
                var service = new Service { Name = "Corte", DurationMinutes = 30, Price = 10m, Business = business };
                var employee = new Employee { FullName = "Luis", Business = business, IsActive = true, MaxConcurrentAppointments = 1 };
                ctx.AddRange(business, client, service, employee);
                await ctx.SaveChangesAsync();

                var appointment = new Appointment
                {
                    ClientId = client.Id,
                    EmployeeId = employee.Id,
                    ServiceId = service.Id,
                    StartDate = new DateTime(2027, 1, 4, 9, 0, 0, DateTimeKind.Utc),
                    EndDate = new DateTime(2027, 1, 4, 9, 30, 0, DateTimeKind.Utc),
                    Status = AppointmentStatus.Confirmed
                };
                ctx.Appointments.Add(appointment);
                await ctx.SaveChangesAsync();
                appointmentId = appointment.Id;

                // Soft-delete the client AFTER the appointment exists.
                client.IsDeleted = true;
                ctx.Clients.Update(client);
                await ctx.SaveChangesAsync();
            }

            using (var ctx = NewContext(dbName))
            {
                var repo = new AppointmentRepository(ctx);
                var loaded = await repo.GetByIdWithDetailsAsync(appointmentId);

                Assert.NotNull(loaded);
                // The required navigations must still load (history is kept), even
                // though the client was soft-deleted.
                Assert.NotNull(loaded!.Client);
                Assert.Equal("Ana", loaded.Client.Name);
                Assert.NotNull(loaded.Service);
                Assert.NotNull(loaded.Employee);
                Assert.NotNull(loaded.Employee.Business);
            }
        }
    }
}
