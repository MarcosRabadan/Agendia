using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Events;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Infrastructure.Messaging;
using MRC.Agendia.Infrastructure.Notifications;
using MRC.Agendia.Tests.Unit.TestDoubles;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Notifications
{
    /// <summary>
    /// Unit tests for <see cref="ReminderProcessor"/> (in-memory context): a due appointment
    /// gets a reminder raised (enlisted into the outbox by the DbContext SaveChanges override)
    /// and ReminderSentAt marked; an already-reminded one is skipped, and one outside the
    /// window is skipped. The advisory lock only runs on a real database.
    /// </summary>
    public class ReminderProcessorTests
    {
        private static readonly DateTime Now = new(2030, 6, 1, 10, 0, 0, DateTimeKind.Unspecified);

        private static AgendiaDbContext NewContext(string dbName) =>
            new(new DbContextOptionsBuilder<AgendiaDbContext>()
                .UseInMemoryDatabase(dbName)
                .ConfigureWarnings(w => w.Ignore(
                    CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning))
                .Options, new UnrestrictedBusinessScope());

        private static ReminderProcessor NewProcessor(AgendiaDbContext ctx, IClock clock) =>
            new(ctx, clock, Options.Create(new ReminderOptions { ReminderWindowHours = 24 }),
                NullLogger<ReminderProcessor>.Instance);

        private static IClock ClockAt(DateTime now)
        {
            var clock = Substitute.For<IClock>();
            clock.BusinessNow.Returns(now);
            return clock;
        }

        private static async Task<Appointment> SeedAppointmentAsync(AgendiaDbContext ctx, DateTime start, DateTime? reminderSentAt = null)
        {
            var business = new Business { IsActive = true, DefaultLanguage = "es" };
            var employee = new Employee { Business = business, IsActive = true, MaxConcurrentAppointments = 1 };
            var service = new Service { Business = business, DurationMinutes = 30 };
            var appointment = new Appointment
            {
                ClientUserId = "harmony-x",
                Employee = employee,
                Service = service,
                StartDate = start,
                EndDate = start.AddMinutes(30),
                Status = AppointmentStatus.Confirmed,
                ReminderSentAt = reminderSentAt
            };
            ctx.AddRange(business, employee, service, appointment);
            await ctx.SaveChangesAsync();
            return appointment;
        }

        [Fact]
        public async Task ProcessDueAsync_DueAppointment_RaisesReminder_AndMarks()
        {
            await using var ctx = NewContext(nameof(ProcessDueAsync_DueAppointment_RaisesReminder_AndMarks));
            var appt = await SeedAppointmentAsync(ctx, Now.AddHours(2)); // within the 24h window

            var published = await NewProcessor(ctx, ClockAt(Now)).ProcessDueAsync();

            Assert.Equal(1, published);
            var stored = await ctx.Appointments.FindAsync(appt.Id);
            Assert.NotNull(stored!.ReminderSentAt);
            // The reminder was raised on the appointment and enlisted into the outbox on save.
            var reminders = await ctx.Set<OutboxMessage>()
                .CountAsync(m => m.Type == nameof(AppointmentReminder) && m.Payload.Contains(appt.Id.ToString()));
            Assert.Equal(1, reminders);
        }

        [Fact]
        public async Task ProcessDueAsync_AlreadyReminded_DoesNothing()
        {
            await using var ctx = NewContext(nameof(ProcessDueAsync_AlreadyReminded_DoesNothing));
            await SeedAppointmentAsync(ctx, Now.AddHours(2), reminderSentAt: DateTime.UtcNow);

            var published = await NewProcessor(ctx, ClockAt(Now)).ProcessDueAsync();

            Assert.Equal(0, published);
            Assert.Equal(0, await ctx.Set<OutboxMessage>().CountAsync(m => m.Type == nameof(AppointmentReminder)));
        }

        [Fact]
        public async Task ProcessDueAsync_OutsideWindow_DoesNothing()
        {
            await using var ctx = NewContext(nameof(ProcessDueAsync_OutsideWindow_DoesNothing));
            await SeedAppointmentAsync(ctx, Now.AddHours(48)); // beyond the 24h window

            var published = await NewProcessor(ctx, ClockAt(Now)).ProcessDueAsync();

            Assert.Equal(0, published);
            Assert.Equal(0, await ctx.Set<OutboxMessage>().CountAsync(m => m.Type == nameof(AppointmentReminder)));
        }
    }
}
