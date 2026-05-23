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
    }
}
