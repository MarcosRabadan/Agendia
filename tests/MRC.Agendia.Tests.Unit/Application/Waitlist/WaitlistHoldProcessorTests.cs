using Microsoft.Extensions.Logging.Abstractions;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Application.Appointments;
using MRC.Agendia.Application.Availability;
using MRC.Agendia.Application.Waitlist;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Events;
using MRC.Agendia.Domain.Interfaces;
using NSubstitute;
using BusinessEntity = MRC.Agendia.Domain.Entities.Business;

namespace MRC.Agendia.Tests.Unit.Application.Waitlist
{
    /// <summary>
    /// Unit tests for the hold expiry sweep (#268): a hold that ran out is marked Expired
    /// and the slot moves to the next client in the queue, but only while it is really
    /// free.
    /// </summary>
    public class WaitlistHoldProcessorTests
    {
        private static readonly Guid BusinessId = TestIds.Of(1);
        private static readonly Guid ServiceId = TestIds.Of(2);
        private static readonly Guid EmployeeId = TestIds.Of(3);
        private static readonly DateOnly Date = new(2035, 6, 4);
        private static readonly TimeOnly StartTime = new(10, 0);

        private readonly IWaitlistRepository _repository = Substitute.For<IWaitlistRepository>();
        private readonly IAvailabilityService _availability = Substitute.For<IAvailabilityService>();
        private readonly IBusinessRepository _businessRepository = Substitute.For<IBusinessRepository>();
        private readonly IBookingConcurrencyGuard _guard = Substitute.For<IBookingConcurrencyGuard>();
        private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
        private readonly IClock _clock = Substitute.For<IClock>();
        private readonly WaitlistHoldProcessor _sut;

        public WaitlistHoldProcessorTests()
        {
            _clock.TimeZoneId.Returns("Europe/Madrid");
            // The guard just runs the critical section directly in unit tests.
            _guard.ExecuteSerializedAsync(Arg.Any<Guid>(), Arg.Any<DateOnly>(), Arg.Any<Func<Task>>(), Arg.Any<CancellationToken>())
                .Returns(ci => ci.Arg<Func<Task>>()());
            _businessRepository.GetActiveByIdAsync(BusinessId, Arg.Any<CancellationToken>())
                .Returns(new BusinessEntity { DefaultLanguage = "es" });

            _sut = new WaitlistHoldProcessor(_repository, _availability, _businessRepository, _guard, _unitOfWork,
                _clock, new WaitlistOptions(), NullLogger<WaitlistHoldProcessor>.Instance);
        }

        [Fact]
        public async Task An_expired_hold_moves_the_slot_to_the_next_in_the_queue()
        {
            var expired = Entry("client-1", WaitlistStatus.Notified, DateTime.UtcNow.AddMinutes(-1));
            var next = Entry("client-2", WaitlistStatus.Waiting);
            _repository.GetExpiredHoldsAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(new List<WaitlistEntry> { expired });
            _repository.GetNextWaitingForSlotAsync(BusinessId, ServiceId, Date, StartTime, EmployeeId, Arg.Any<CancellationToken>())
                .Returns(next);
            _availability.GetSlotCapacityAsync(BusinessId, Date, StartTime, ServiceId, EmployeeId, Arg.Any<CancellationToken>())
                .Returns(1);

            var processed = await _sut.ExpireDueHoldsAsync();

            Assert.Equal(1, processed);
            Assert.Equal(WaitlistStatus.Expired, expired.Status);

            // The next client is now the one holding it, with a fresh window and its event.
            Assert.Equal(WaitlistStatus.Notified, next.Status);
            Assert.NotNull(next.HoldUntil);
            var raised = Assert.Single(next.DomainEvents.OfType<WaitlistSlotAvailable>());
            Assert.Equal(next.HoldUntil, raised.HoldUntil);
        }

        [Fact]
        public async Task A_slot_that_filled_up_again_expires_the_hold_without_notifying()
        {
            var expired = Entry("client-1", WaitlistStatus.Notified, DateTime.UtcNow.AddMinutes(-1));
            var next = Entry("client-2", WaitlistStatus.Waiting);
            _repository.GetExpiredHoldsAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(new List<WaitlistEntry> { expired });
            _repository.GetNextWaitingForSlotAsync(BusinessId, ServiceId, Date, StartTime, EmployeeId, Arg.Any<CancellationToken>())
                .Returns(next);
            // Someone booked it in the meantime.
            _availability.GetSlotCapacityAsync(BusinessId, Date, StartTime, ServiceId, EmployeeId, Arg.Any<CancellationToken>())
                .Returns(0);

            await _sut.ExpireDueHoldsAsync();

            Assert.Equal(WaitlistStatus.Expired, expired.Status);
            Assert.Equal(WaitlistStatus.Waiting, next.Status);
            Assert.Empty(next.DomainEvents);
        }

        [Fact]
        public async Task Nothing_due_does_nothing()
        {
            _repository.GetExpiredHoldsAsync(Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(new List<WaitlistEntry>());

            Assert.Equal(0, await _sut.ExpireDueHoldsAsync());
            await _unitOfWork.DidNotReceiveWithAnyArgs().Save();
        }

        private static WaitlistEntry Entry(string clientUserId, WaitlistStatus status, DateTime? holdUntil = null) => new()
        {
            Id = Guid.NewGuid(),
            BusinessId = BusinessId,
            ServiceId = ServiceId,
            EmployeeId = EmployeeId,
            ClientUserId = clientUserId,
            Date = Date,
            StartTime = StartTime,
            Status = status,
            HoldUntil = holdUntil,
            CreatedAt = DateTime.UtcNow
        };
    }
}
