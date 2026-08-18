using MRC.Agendia.Domain.Entities;
using MRC.Agendia.Domain.Services;

namespace MRC.Agendia.Tests.Unit.Domain
{
    /// <summary>
    /// Unit tests for the shared template tie-break (#307). The rule used to live in four
    /// copies that stopped at <c>IsDefault</c>, so two overlapping templates resolved
    /// differently depending on whether the answer came from the database, the cache or an
    /// in-memory list. What matters here is that the order is TOTAL: the same set must give
    /// the same winner however it arrives.
    /// </summary>
    public class ScheduleTemplateSelectionTests
    {
        private static readonly DateOnly Date = new(2035, 6, 4);

        private static ScheduleTemplate Template(int id, bool isDefault, DateOnly from, DateOnly to) => new()
        {
            Id = TestIds.Of(id),
            BusinessId = TestIds.Of(99),
            Name = $"T{id}",
            IsDefault = isDefault,
            EffectiveFrom = from,
            EffectiveTo = to
        };

        private static ScheduleTemplate Covering(int id, bool isDefault = false)
            => Template(id, isDefault, new DateOnly(2035, 1, 1), new DateOnly(2035, 12, 31));

        [Fact]
        public void No_template_covering_the_date_resolves_to_nothing()
        {
            var outside = Template(1, true, new DateOnly(2034, 1, 1), new DateOnly(2034, 12, 31));

            Assert.Null(ScheduleTemplateSelection.SelectFor(new[] { outside }, Date));
        }

        [Fact]
        public void Only_the_templates_covering_the_date_are_considered()
        {
            var outside = Template(1, true, new DateOnly(2034, 1, 1), new DateOnly(2034, 12, 31));
            var inside = Covering(2);

            var winner = ScheduleTemplateSelection.SelectFor(new[] { outside, inside }, Date);

            Assert.Equal(inside.Id, winner!.Id);
        }

        [Fact]
        public void A_default_template_beats_a_non_default_one()
        {
            var plain = Covering(1);
            var isDefault = Covering(2, isDefault: true);

            var winner = ScheduleTemplateSelection.SelectFor(new[] { plain, isDefault }, Date);

            Assert.Equal(isDefault.Id, winner!.Id);
        }

        [Fact]
        public void The_template_starting_latest_wins_when_neither_is_default()
        {
            // The later range is the more specific one for this date.
            var wide = Template(1, false, new DateOnly(2035, 1, 1), new DateOnly(2035, 12, 31));
            var narrow = Template(2, false, new DateOnly(2035, 6, 1), new DateOnly(2035, 6, 30));

            var winner = ScheduleTemplateSelection.SelectFor(new[] { wide, narrow }, Date);

            Assert.Equal(narrow.Id, winner!.Id);
        }

        [Fact]
        public void The_input_order_does_not_change_the_winner()
        {
            // Two templates tied on everything the rule looks at except the id: this is the
            // case that used to resolve at random, because the sources disagreed on order.
            var templates = new[]
            {
                Covering(1),
                Covering(2),
                Template(3, false, new DateOnly(2035, 3, 1), new DateOnly(2035, 9, 30))
            };

            var forward = ScheduleTemplateSelection.SelectFor(templates, Date);
            var reversed = ScheduleTemplateSelection.SelectFor(templates.Reverse().ToList(), Date);

            Assert.NotNull(forward);
            Assert.Equal(forward!.Id, reversed!.Id);
        }

        [Fact]
        public void Templates_tied_on_range_and_default_still_resolve_to_one_of_them()
        {
            var first = Covering(1, isDefault: true);
            var second = Covering(2, isDefault: true);

            var forward = ScheduleTemplateSelection.SelectFor(new[] { first, second }, Date);
            var reversed = ScheduleTemplateSelection.SelectFor(new[] { second, first }, Date);

            // The database index now stops two defaults from coexisting, but the rule must
            // not depend on that: it has to stay total on its own.
            Assert.Equal(forward!.Id, reversed!.Id);
        }
    }
}
