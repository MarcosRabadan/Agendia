using MRC.Agendia.Domain.Entities;

namespace MRC.Agendia.Domain.Services
{
    /// <summary>
    /// The one rule that decides WHICH template governs a given date (#307).
    ///
    /// <para>It used to be copy-pasted in four places - the resolver, the repository, the
    /// caching decorator and <c>ScheduleService</c> - and none of them ordered past
    /// <c>IsDefault</c>. Two templates whose ranges overlap therefore resolved differently
    /// depending on who asked: the DB returns rows in no guaranteed order and the cache
    /// serves whatever order the list was built in. Availability could offer 09:00-14:00
    /// while the booking validator enforced 16:00-20:00 for the same day.</para>
    ///
    /// <para>The order is <b>total</b>, so it cannot tie: default first, then the template
    /// that starts latest (the more specific one for that date), then the id as a last
    /// resort. Whether the winner is the "right" one for a business that has overlapping
    /// templates is a separate question - the point here is that everyone picks the SAME
    /// one.</para>
    /// </summary>
    public static class ScheduleTemplateSelection
    {
        /// <summary>
        /// Picks the template that governs <paramref name="date"/>, or null when none covers it.
        /// </summary>
        /// <param name="templates">Candidate templates, typically a business's whole set.</param>
        /// <param name="date">Date to resolve.</param>
        /// <returns>The governing template, or null.</returns>
        public static ScheduleTemplate? SelectFor(IEnumerable<ScheduleTemplate> templates, DateOnly date)
            => templates
                .Where(t => t.EffectiveFrom <= date && t.EffectiveTo >= date)
                .OrderByDescending(t => t.IsDefault)
                .ThenByDescending(t => t.EffectiveFrom)
                .ThenBy(t => t.Id)
                .FirstOrDefault();
    }
}
