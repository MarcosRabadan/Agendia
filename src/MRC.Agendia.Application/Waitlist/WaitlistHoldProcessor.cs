using Microsoft.Extensions.Logging;
using MRC.Agendia.Application.Appointments;
using MRC.Agendia.Application.Availability;
using MRC.Agendia.Application.Common;
using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Events;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Waitlist
{
    /// <summary>
    /// Sweeps the priority holds whose window ran out (#268): the entry is marked Expired
    /// and the queue moves on to the next client waiting for that slot, FIFO, provided the
    /// slot is still free.
    ///
    /// <para>The per-slot work runs inside <see cref="IBookingConcurrencyGuard"/>, keyed by
    /// employee + day like every other booking-critical section, so an expiry cannot
    /// interleave with a booking or with another cancellation notifying the same slot. An
    /// "any employee" entry has no employee to key on, so it is serialized on the business
    /// id instead - a coarser lock, but the same guarantee.</para>
    ///
    /// <para>This owns the work of one cycle; the timing loop lives in the hosted service,
    /// which keeps this testable.</para>
    /// </summary>
    public class WaitlistHoldProcessor
    {
        private readonly IWaitlistRepository _repository;
        private readonly IAvailabilityService _availabilityService;
        private readonly IBusinessRepository _businessRepository;
        private readonly IBookingConcurrencyGuard _bookingGuard;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IClock _clock;
        private readonly WaitlistOptions _options;
        private readonly ILogger<WaitlistHoldProcessor> _logger;

        public WaitlistHoldProcessor(IWaitlistRepository repository,
                                     IAvailabilityService availabilityService,
                                     IBusinessRepository businessRepository,
                                     IBookingConcurrencyGuard bookingGuard,
                                     IUnitOfWork unitOfWork,
                                     IClock clock,
                                     WaitlistOptions options,
                                     ILogger<WaitlistHoldProcessor> logger)
        {
            _repository = repository;
            _availabilityService = availabilityService;
            _businessRepository = businessRepository;
            _bookingGuard = bookingGuard;
            _unitOfWork = unitOfWork;
            _clock = clock;
            _options = options;
            _logger = logger;
        }

        /// <summary>
        /// Expires the holds that ran out and passes each freed slot to the next client in
        /// the queue. Returns how many holds were expired.
        /// </summary>
        public async Task<int> ExpireDueHoldsAsync(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;
            var expired = await _repository.GetExpiredHoldsAsync(now, Math.Max(1, _options.ExpiryBatchSize), cancellationToken);
            if (expired.Count == 0)
                return 0;

            foreach (var entry in expired)
            {
                var lockKey = entry.EmployeeId ?? entry.BusinessId;

                await _bookingGuard.ExecuteSerializedAsync(lockKey, entry.Date, async () =>
                {
                    entry.Status = WaitlistStatus.Expired;
                    _repository.Update(entry);
                    await _unitOfWork.Save(cancellationToken);

                    await NotifyNextAsync(entry, cancellationToken);
                }, cancellationToken);
            }

            _logger.LogInformation("Expired {Count} waitlist hold(s).", expired.Count);
            return expired.Count;
        }

        // Same shape as the notification on a freed appointment: take everybody whose slot
        // overlaps the one this hold was keeping, re-check the room is really there, and hand
        // it to the first who fits with a fresh hold.
        private async Task NotifyNextAsync(WaitlistEntry expired, CancellationToken cancellationToken)
        {
            // GetWaitingCandidatesForSlotAsync matches "any employee" entries too, so an entry
            // without an employee needs one to query with; there is none, and the capacity
            // check below is what decides anyway.
            if (expired.EmployeeId is not Guid employee)
                return;

            // The window this hold was keeping, and everybody whose own slot overlaps it (#350).
            // Service comes loaded with the expired hold precisely for this.
            var freedStart = expired.Date.ToDateTime(expired.StartTime);
            var duration = expired.Service.DurationMinutes;
            var (windowEnd, earliestStart) = WaitlistSlotWindow.OverlapBounds(
                freedStart, freedStart.AddMinutes(duration), duration);

            var candidates = await _repository.GetWaitingCandidatesForSlotAsync(
                expired.BusinessId, expired.ServiceId, expired.Date, windowEnd, earliestStart,
                employee, Math.Max(1, _options.NotifyCandidateLimit), cancellationToken);

            // First one that FITS, not simply the first: a candidate whose own slot is still
            // blocked must not starve the ones behind it.
            foreach (var next in candidates)
            {
                var capacity = await _availabilityService.GetSlotCapacityAsync(
                    next.BusinessId, next.Date, next.StartTime, next.ServiceId, next.EmployeeId, cancellationToken);
                if (capacity is null or <= 0)
                    continue;

                var business = await _businessRepository.GetActiveByIdAsync(next.BusinessId, cancellationToken);
                var holdUntil = DateTime.UtcNow.AddMinutes(Math.Max(1, _options.HoldMinutes));

                next.RaiseEvent(new WaitlistSlotAvailable(
                    next.Id, next.BusinessId, next.EmployeeId, next.ClientUserId,
                    next.ServiceId, next.Date, next.StartTime, holdUntil,
                    business?.DefaultLanguage ?? "es", _clock.TimeZoneId, DateTime.UtcNow));

                next.Status = WaitlistStatus.Notified;
                next.HoldUntil = holdUntil;
                _repository.Update(next);
                await _unitOfWork.Save(cancellationToken);

                // One freed seat, one notification.
                break;
            }
        }
    }
}
