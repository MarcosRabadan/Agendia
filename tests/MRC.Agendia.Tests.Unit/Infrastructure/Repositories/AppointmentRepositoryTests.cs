using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Infrastructure.Repositories;
using MRC.Agendia.Tests.Unit.TestDoubles;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Repositories
{
    public class AppointmentRepositoryTests
    {
        private static readonly DateTime Start = new(2027, 1, 4, 9, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime End = new(2027, 1, 4, 9, 30, 0, DateTimeKind.Utc);

        private static AgendiaDbContext NewContext(string dbName) =>
            new(new DbContextOptionsBuilder<AgendiaDbContext>()
                .UseInMemoryDatabase(dbName)
                .ConfigureWarnings(w => w.Ignore(
                    CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
                .Options, new UnrestrictedBusinessScope());

        private sealed record Seeded(int AppointmentId, int BusinessId, int ClientId, int EmployeeId, int ServiceId);

        /// <summary>Seeds business + client + service + employee + one confirmed appointment.</summary>
        private static async Task<Seeded> SeedAsync(AgendiaDbContext ctx)
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
                StartDate = Start,
                EndDate = End,
                Status = AppointmentStatus.Confirmed
            };
            ctx.Appointments.Add(appointment);
            await ctx.SaveChangesAsync();

            return new Seeded(appointment.Id, business.Id, client.Id, employee.Id, service.Id);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_CargaPadresAunqueEstenSoftDeleted()
        {
            var dbName = $"appt-repo-{Guid.NewGuid()}";
            Seeded seeded;
            using (var ctx = NewContext(dbName))
            {
                seeded = await SeedAsync(ctx);
                var client = await ctx.Clients.FindAsync(seeded.ClientId);
                client!.IsDeleted = true;
                await ctx.SaveChangesAsync();
            }

            using (var ctx = NewContext(dbName))
            {
                var loaded = await new AppointmentRepository(ctx).GetByIdWithDetailsAsync(seeded.AppointmentId);

                Assert.NotNull(loaded);
                Assert.NotNull(loaded!.Client);
                Assert.Equal("Ana", loaded.Client.Name);
                Assert.NotNull(loaded.Service);
                Assert.NotNull(loaded.Employee.Business);
            }
        }

        [Fact]
        public async Task GetByBusinessIdAndDateRangeAsync_IncluyeCitaConServicioSoftDeleted()
        {
            // Capacity/conflict check must keep counting a live appointment even if
            // its service was soft-deleted; otherwise the slot looks free -> double-booking.
            var dbName = $"appt-repo-{Guid.NewGuid()}";
            Seeded seeded;
            using (var ctx = NewContext(dbName))
            {
                seeded = await SeedAsync(ctx);
                var service = await ctx.Services.FindAsync(seeded.ServiceId);
                service!.IsDeleted = true;
                await ctx.SaveChangesAsync();
            }

            using (var ctx = NewContext(dbName))
            {
                var result = await new AppointmentRepository(ctx)
                    .GetByBusinessIdAndDateRangeAsync(seeded.BusinessId, Start.Date, Start.Date.AddDays(1));

                Assert.Single(result);
            }
        }

        [Fact]
        public async Task GetPagedAsync_IncluyeCitaConEmpleadoSoftDeleted()
        {
            var dbName = $"appt-repo-{Guid.NewGuid()}";
            Seeded seeded;
            using (var ctx = NewContext(dbName))
            {
                seeded = await SeedAsync(ctx);
                var employee = await ctx.Employees.FindAsync(seeded.EmployeeId);
                employee!.IsDeleted = true;
                await ctx.SaveChangesAsync();
            }

            using (var ctx = NewContext(dbName))
            {
                var (items, total) = await new AppointmentRepository(ctx).GetPagedAsync(1, 50);

                Assert.Equal(1, total);
                Assert.Single(items);
            }
        }

        [Fact]
        public async Task GetByBusinessIdAndDateRangeAsync_DevuelveCitaQueSolapaElBorde()
        {
            // Appointment 09:00-09:30; a range starting AFTER 09:00 but before its
            // end must still return it (overlap, not containment) - issue BIZ-04.
            var dbName = $"appt-repo-{Guid.NewGuid()}";
            Seeded seeded;
            using (var ctx = NewContext(dbName))
            {
                seeded = await SeedAsync(ctx);
            }

            using (var ctx = NewContext(dbName))
            {
                var from = new DateTime(2027, 1, 4, 9, 15, 0, DateTimeKind.Utc);
                var to = new DateTime(2027, 1, 4, 12, 0, 0, DateTimeKind.Utc);

                var result = await new AppointmentRepository(ctx)
                    .GetByBusinessIdAndDateRangeAsync(seeded.BusinessId, from, to);

                Assert.Single(result);
            }
        }

        [Fact]
        public async Task GetByBusinessIdAndDateRangeAsync_ExcluyeCitaSoftDeleted()
        {
            // The appointment's OWN soft-delete still hides it.
            var dbName = $"appt-repo-{Guid.NewGuid()}";
            Seeded seeded;
            using (var ctx = NewContext(dbName))
            {
                seeded = await SeedAsync(ctx);
                var appointment = await ctx.Appointments.FindAsync(seeded.AppointmentId);
                appointment!.IsDeleted = true;
                await ctx.SaveChangesAsync();
            }

            using (var ctx = NewContext(dbName))
            {
                var result = await new AppointmentRepository(ctx)
                    .GetByBusinessIdAndDateRangeAsync(seeded.BusinessId, Start.Date, Start.Date.AddDays(1));

                Assert.Empty(result);
            }
        }
    }
}
