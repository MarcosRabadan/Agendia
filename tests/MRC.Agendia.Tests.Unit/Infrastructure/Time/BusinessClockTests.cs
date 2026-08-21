using Microsoft.Extensions.Configuration;
using MRC.Agendia.Infrastructure.Time;
using NSubstitute;

namespace MRC.Agendia.Tests.Unit.Infrastructure.Time
{
    public class BusinessClockTests
    {
        private static IConfiguration ConfigWith(string? timeZone)
        {
            var config = Substitute.For<IConfiguration>();
            config["Scheduling:TimeZone"].Returns(timeZone);
            return config;
        }

        [Fact]
        public void BusinessNow_AplicaElOffsetDeLaZonaConfigurada()
        {
            var clock = new BusinessClock(ConfigWith("Europe/Madrid"));
            var madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

            var utc = DateTime.UtcNow;
            var businessNow = clock.BusinessNow;

            var expectedOffset = madrid.GetUtcOffset(utc);
            Assert.Equal(DateTimeKind.Unspecified, businessNow.Kind);
            Assert.True(
                Math.Abs((businessNow - utc - expectedOffset).TotalSeconds) < 5,
                $"BusinessNow ({businessNow:o}) deberia aplicar el offset de Madrid ({expectedOffset}) sobre UTC ({utc:o}).");
        }

        [Fact]
        public void Constructor_ZonaInexistente_Lanza()
        {
            Assert.Throws<InvalidOperationException>(() => new BusinessClock(ConfigWith("Zona/Inexistente")));
        }

        // #321: the id travels in every event payload, so it must be the CONFIGURED (IANA) one.
        // TimeZoneInfo.Id would report the Windows id ("Romance Standard Time") on Windows and
        // the IANA one on Linux, putting a different value in the payload per host.
        [Fact]
        public void TimeZoneId_ReportsTheConfiguredIanaId_NotTheHostSpecificOne()
        {
            var clock = new BusinessClock(ConfigWith("Europe/Madrid"));

            Assert.Equal("Europe/Madrid", clock.TimeZoneId);
        }

        [Fact]
        public void TimeZoneId_SinConfigurar_CaeAlPorDefecto()
        {
            Assert.Equal("Europe/Madrid", new BusinessClock(ConfigWith(null)).TimeZoneId);
        }
    }
}
