using MRC.Agendia.Application.Waitlist;

namespace MRC.Agendia.Tests.Unit.Application.Waitlist
{
    /// <summary>
    /// The overlap geometry the waitlist notification selects candidates with (#350). It lives on
    /// its own because both paths that notify need it, and because its two edges - a window that
    /// runs into the next day, and one that opens less than a duration after midnight - are
    /// exactly where naive TimeOnly arithmetic wraps round and silently matches nobody.
    /// </summary>
    public class WaitlistSlotWindowTests
    {
        private static readonly DateOnly Day = new(2030, 6, 7);

        [Fact]
        public void OverlapBounds_VentanaNormal_DaLasDosCotas()
        {
            var start = Day.ToDateTime(new TimeOnly(10, 0));

            var (windowEnd, earliestStart) = WaitlistSlotWindow.OverlapBounds(
                start, start.AddMinutes(60), serviceDurationMinutes: 60);

            // A 10:00-11:00 class with 60 minute slots: 10:30 overlaps, 09:00 and 11:00 do not.
            Assert.Equal(new TimeOnly(11, 0), windowEnd);
            Assert.Equal(new TimeOnly(9, 0), earliestStart);
        }

        [Fact]
        public void OverlapBounds_ClaseMasLargaQueElServicio_UsaLaVentanaEntera()
        {
            // Multiservice (#170): the booking lasts longer than the service the queue waits for,
            // and the seat frees for the WHOLE booking.
            var start = Day.ToDateTime(new TimeOnly(10, 0));

            var (windowEnd, earliestStart) = WaitlistSlotWindow.OverlapBounds(
                start, start.AddMinutes(90), serviceDurationMinutes: 30);

            Assert.Equal(new TimeOnly(11, 30), windowEnd);
            Assert.Equal(new TimeOnly(9, 30), earliestStart);
        }

        [Fact]
        public void OverlapBounds_VentanaQueCruzaMedianoche_SinCotaSuperior()
        {
            // 23:30 + 60 min lands on the next day. Its time of day reads as 00:30, which as an
            // upper bound would exclude everybody instead of nobody.
            var start = Day.ToDateTime(new TimeOnly(23, 30));

            var (windowEnd, earliestStart) = WaitlistSlotWindow.OverlapBounds(
                start, start.AddMinutes(60), serviceDurationMinutes: 60);

            Assert.Null(windowEnd);
            Assert.Equal(new TimeOnly(22, 30), earliestStart);
        }

        [Fact]
        public void OverlapBounds_CercaDeMedianoche_SinCotaInferior()
        {
            // 00:30 - 60 min would wrap round to 23:30 and match nobody. Nothing to exclude: every
            // possible start time is late enough to still be running at 00:30.
            var start = Day.ToDateTime(new TimeOnly(0, 30));

            var (windowEnd, earliestStart) = WaitlistSlotWindow.OverlapBounds(
                start, start.AddMinutes(60), serviceDurationMinutes: 60);

            Assert.Equal(new TimeOnly(1, 30), windowEnd);
            Assert.Null(earliestStart);
        }

        [Fact]
        public void OverlapBounds_ExactamenteUnaDuracionTrasMedianoche_ExcluyeElDeMedianoche()
        {
            // 01:00 with 60 minute slots: the 00:00 entry ends exactly when this one starts, so
            // they touch without overlapping and the bound is exclusive.
            var start = Day.ToDateTime(new TimeOnly(1, 0));

            var (_, earliestStart) = WaitlistSlotWindow.OverlapBounds(
                start, start.AddMinutes(60), serviceDurationMinutes: 60);

            Assert.Equal(TimeOnly.MinValue, earliestStart);
        }
    }
}
