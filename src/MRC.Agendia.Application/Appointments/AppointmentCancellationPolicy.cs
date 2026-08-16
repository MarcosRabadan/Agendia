using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Enums;
using MRC.Agendia.Domain.Exceptions;
using MRC.Agendia.Domain.Interfaces;

namespace MRC.Agendia.Application.Appointments
{
    /// <summary>
    /// Self-service cancellation/reschedule rule. A business states it either as a single
    /// advance-notice threshold (<c>CancellationWindowHours</c>) or, since #270, as a list
    /// of tiers with a growing consequence ("free 24h ahead, 50% from 4h, not allowed
    /// after that"). Staff are never subject to it. Pure and side-effect free so it can be
    /// unit-tested in isolation.
    /// </summary>
    public static class AppointmentCancellationPolicy
    {
        /// <summary>
        /// Applies the business's rule to a self-service cancellation/reschedule and
        /// returns the tier that applied, or null when the business has no tiers (it is
        /// then the plain single-threshold rule, which has no tier to report).
        /// Throws <see cref="CancellationWindowElapsedException"/> when the action is not
        /// allowed this close to the appointment.
        /// </summary>
        /// <param name="appointmentStart">Wall-clock start of the appointment being cancelled/moved.</param>
        /// <param name="policy">The business's window and tiers.</param>
        /// <param name="now">Current business wall-clock time.</param>
        public static CancellationPolicyTier? EnsureSelfServiceAllowed(DateTime appointmentStart,
                                                                       CancellationPolicySnapshot policy,
                                                                       DateTime now)
        {
            // No tiers: the business is still on the single-threshold rule.
            if (policy.Tiers.Count == 0)
            {
                EnsureSelfServiceAllowed(appointmentStart, policy.WindowHours, now);
                return null;
            }

            // The applying tier is the most demanding threshold the client still meets.
            // The set always covers 0 hours (enforced when the policy is saved), so there
            // is always exactly one.
            var hoursAhead = (appointmentStart - now).TotalHours;
            var applied = policy.Tiers
                .Where(t => hoursAhead >= t.MinHoursBefore)
                .OrderByDescending(t => t.MinHoursBefore)
                .FirstOrDefault();

            // Defensive: a policy that does not reach down to 0 leaves the last stretch
            // uncovered. Treat it as the strictest tier rather than as "anything goes".
            applied ??= policy.Tiers.OrderBy(t => t.MinHoursBefore).First();

            if (applied.PenaltyKind == CancellationPenaltyKind.NotAllowed)
            {
                // Report the notice the client would have needed: the next threshold up,
                // which is the last one that still allowed acting on their own.
                var requiredNotice = policy.Tiers
                    .Where(t => t.MinHoursBefore > applied.MinHoursBefore)
                    .OrderBy(t => t.MinHoursBefore)
                    .Select(t => t.MinHoursBefore)
                    .FirstOrDefault(applied.MinHoursBefore);

                throw new CancellationWindowElapsedException(requiredNotice);
            }

            return applied;
        }

        /// <summary>
        /// Throws <see cref="CancellationWindowElapsedException"/> when self-service
        /// cancellation/reschedule is no longer allowed because the appointment is
        /// within the business's advance-notice window. A null or non-positive
        /// window means no restriction.
        /// </summary>
        /// <param name="appointmentStart">Wall-clock start of the appointment being cancelled/moved.</param>
        /// <param name="windowHours">The business's configured window, or null for no restriction.</param>
        /// <param name="now">Current business wall-clock time.</param>
        public static void EnsureSelfServiceAllowed(DateTime appointmentStart, int? windowHours, DateTime now)
        {
            if (windowHours is null || windowHours.Value <= 0)
                return;

            // Latest moment the client may still act on their own. Past it, only staff can.
            var deadline = appointmentStart.AddHours(-windowHours.Value);
            if (now > deadline)
                throw new CancellationWindowElapsedException(windowHours.Value);
        }
    }
}
