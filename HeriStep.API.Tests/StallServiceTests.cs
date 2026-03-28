using FluentAssertions;
using HeriStep.API.Data;
using HeriStep.API.Services;
using HeriStep.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HeriStep.API.Tests
{
    [TestFixture]
    public class TtsGenerationTests
    {
        [Test]
        public void ExtendSubscription_WhenStallHasNoSub_ShouldCreateNewSub()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<HeriStepDbContext>()
                .UseInMemoryDatabase(databaseName: "SmartTravel_TestDb")
                .Options;

            using (var dbContext = new HeriStepDbContext(options))
            {
                var mockCache = new Mock<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
                var service = new StallService(dbContext, mockCache.Object);

                // Act
                var result = service.ExtendSubscriptionAsync(1).Result;

                // Assert
                result.Should().BeTrue();
                var sub = dbContext.Subscriptions.FirstOrDefault(s => s.StallId == 1);
                sub.Should().NotBeNull();
                sub.IsActive.Should().BeTrue();
                sub.ExpiryDate.Should().BeAfter(DateTime.Now.AddDays(29));
            }
        }
    }
}
