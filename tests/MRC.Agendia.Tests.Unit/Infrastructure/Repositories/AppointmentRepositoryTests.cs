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

        private sealed record Seeded(Guid AppointmentId, Guid BusinessId, Guid EmployeeId, Guid ServiceId);

        /// <summary>Seeds business + service + employee + one confirmed appointment.</summary>
        private static async Task<Seeded> SeedAsync(AgendiaDbContext ctx)
        {
            var business = new Business { IsActive = true };
            var service = new Service { DurationMinutes = 30, Business = business };
            var employee = new Employee { Business = business, IsActive = true, MaxConcurrentAppointments = 1 };
            ctx.AddRange(business, service, employee);
            await ctx.SaveChangesAsync();

            var appointment = new Appointment
            {
                ClientUserId = "harmony-ana",
                EmployeeId = employee.Id,
                ServiceId = service.Id,
                StartDate = Start,
                EndDate = End,
                Status = AppointmentStatus.Confirmed
            };
            ctx.Appointments.Add(appointment);
            await ctx.SaveChangesAsync();

            return new Seeded(appointment.Id, business.Id, employee.Id, service.Id);
        }

        [Fact]
        public async Task GetByIdWithDetailsAsync_CargaPadresAunqueEstenSoftDeleted()
        {
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
                var loaded = await new AppointmentRepository(ctx).GetByIdWithDetailsAsync(seeded.AppointmentId);

                Assert.NotNull(loaded);
                // Required parents (soft-deleted service included) still load via IgnoreQueryFilters.
                Assert.NotNull(loaded!.Service);
                Assert.Equal(30, loaded.Service.DurationMinutes);
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
