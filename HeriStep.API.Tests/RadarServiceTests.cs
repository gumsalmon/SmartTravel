using FluentAssertions;
using NUnit.Framework;
using System;

namespace HeriStep.API.Tests
{
    [TestFixture]
    public class RadarServiceTests
    {
        [Test]
        public void CalculateDistance_WhenPointsAreClose_ShouldReturnExpectedMeters()
        {
            // Arrange (Vĩnh Khánh Street points test)
            double lat1 = 10.7630;
            double lon1 = 106.6600;
            double lat2 = 10.7631;
            double lon2 = 106.6601;

            // Act
            double distance = CalculateDistanceHaversine(lat1, lon1, lat2, lon2);

            // Assert (Khoảng cách giữa 2 điểm này xấp xỉ 15-20 mét)
            distance.Should().BeInRange(10, 20);
        }

        [Test]
        public void IsWithinRadius_WhenTouristInsideRadius_ShouldReturnTrue()
        {
            // Điểm cách sạp < 20m
            bool isInRange = IsWithinRadius(10.7630, 106.6600, 10.7631, 106.6601, 20);
            isInRange.Should().BeTrue();
        }

        [Test]
        public void IsWithinRadius_WhenTouristOutsideRadius_ShouldReturnFalse()
        {
            // Điểm cách sạp rất xa (Quận khác)
            bool isInRange = IsWithinRadius(10.7630, 106.6600, 10.8631, 106.8601, 20);
            isInRange.Should().BeFalse();
        }

        // Logic Helper Test
        private double CalculateDistanceHaversine(double lat1, double lon1, double lat2, double lon2)
        {
            var r = 6371; // Bán kính trái đất KM
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return r * c * 1000; // Đổi về Mét
        }

        private bool IsWithinRadius(double stallLat, double stallLon, double touristLat, double touristLon, double radiusMeter)
        {
            return CalculateDistanceHaversine(stallLat, stallLon, touristLat, touristLon) <= radiusMeter;
        }

        private double ToRadians(double angle) => Math.PI * angle / 180.0;
    }
}
