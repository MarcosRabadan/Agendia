using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Events;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Events;
using MRC.Agendia.Infrastructure;
using MRC.Agendia.Infrastructure.Notifications;
using MRC.Agendia.Tests.Unit.TestDoubles;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Notifications
{
    /// <summary>
    /// Unit tests for <see cref="ReminderProcessor"/> (in-memory context): a due appointment
    /// gets a reminder published and marked, an already-reminded one is skipped, and one
    /// outside the window is skipped. The advisory lock only runs on a real database, so
    /// these exercise the batch logic itself.
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

        private static ReminderProcessor NewProcessor(AgendiaDbContext ctx, IEventPublisher publisher, IClock clock) =>
            new(ctx, publisher, clock, Options.Create(new ReminderOptions { ReminderWindowHours = 24 }),
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
        public async Task ProcessDueAsync_DueAppointment_PublishesReminder_AndMarks()
        {
            await using var ctx = NewContext(nameof(ProcessDueAsync_DueAppointment_PublishesReminder_AndMarks));
            var appt = await SeedAppointmentAsync(ctx, Now.AddHours(2)); // within the 24h window
            var publisher = Substitute.For<IEventPublisher>();

            var published = await NewProcessor(ctx, publisher, ClockAt(Now)).ProcessDueAsync();

            Assert.Equal(1, published);
            await publisher.Received(1).PublishAsync(
                Arg.Is<IIntegrationEvent>(e => e is AppointmentReminder && ((AppointmentReminder)e).AppointmentId == appt.Id),
                Arg.Any<CancellationToken>());
            var stored = await ctx.Appointments.FindAsync(appt.Id);
            Assert.NotNull(stored!.ReminderSentAt);
        }

        [Fact]
        public async Task ProcessDueAsync_AlreadyReminded_DoesNotPublish()
        {
            await using var ctx = NewContext(nameof(ProcessDueAsync_AlreadyReminded_DoesNotPublish));
            await SeedAppointmentAsync(ctx, Now.AddHours(2), reminderSentAt: DateTime.UtcNow);
            var publisher = Substitute.For<IEventPublisher>();

            var published = await NewProcessor(ctx, publisher, ClockAt(Now)).ProcessDueAsync();

            Assert.Equal(0, published);
            await publisher.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
        }

        [Fact]
        public async Task ProcessDueAsync_OutsideWindow_DoesNotPublish()
        {
            await using var ctx = NewContext(nameof(ProcessDueAsync_OutsideWindow_DoesNotPublish));
            await SeedAppointmentAsync(ctx, Now.AddHours(48)); // beyond the 24h window
            var publisher = Substitute.For<IEventPublisher>();

            var published = await NewProcessor(ctx, publisher, ClockAt(Now)).ProcessDueAsync();

            Assert.Equal(0, published);
            await publisher.DidNotReceiveWithAnyArgs().PublishAsync(default!, default);
        }
    }
}
