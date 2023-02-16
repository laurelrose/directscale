using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectScale.Disco.Extension.EventModels;
using Moq;
using NUnit.Framework;
using WebExtension.Controllers;
using WebExtension.Services.DistributedLocking;
using WebExtension.Services.RewardPoints;
using WebExtension.Services;

namespace WebExtensionTests.Controllers
{
    [TestFixture]
    internal class WebHookControllerTests
    {
        private Mock<ICustomLogService> _customLogServiceMock;
        private Mock<IDistributedLockingService> _distributedLockingServiceMock;
        private Mock<IRewardPointService> _rewardPointServiceMock;

        private WebHookController _webHookController;

        [SetUp]
        public void WebHookControllerTestsSetUp()
        {
            _customLogServiceMock = new Mock<ICustomLogService>();
            _distributedLockingServiceMock = new Mock<IDistributedLockingService>();
            _rewardPointServiceMock = new Mock<IRewardPointService>();

            _webHookController = new WebHookController(
                _customLogServiceMock.Object,
                _distributedLockingServiceMock.Object,
                _rewardPointServiceMock.Object
            );
        }

        [Test]
        public async Task WebHookController_DailyEvent_DistributedLockingReturnsNull()
        {
            // Arrange
            _distributedLockingServiceMock
                .Setup(x => x.CreateDistributedLockAsync(It.IsAny<string>(), null))
                .ReturnsAsync((MockSqlDistributedLock)null);

            // Act
            await _webHookController.DailyEvent(new DailyEvent());

            // Assert
            _distributedLockingServiceMock.Verify(x => x.CreateDistributedLockAsync(It.IsAny<string>(), null), Times.Once);
            _rewardPointServiceMock.Verify(x => x.AwardRewardPointCreditsAsync(It.IsAny<int?>()), Times.Never);
        }

        [Test]
        public async Task WebHookController_DailyEvent_DistributedLockingReturnsLock()
        {
            // Arrange
            _distributedLockingServiceMock
                .Setup(x => x.CreateDistributedLockAsync(It.IsAny<string>(), null))
                .ReturnsAsync(new MockSqlDistributedLock());

            // Act
            await _webHookController.DailyEvent(new DailyEvent());

            // Assert
            _distributedLockingServiceMock.Verify(x => x.CreateDistributedLockAsync(It.IsAny<string>(), null), Times.Once);
            _rewardPointServiceMock.Verify(x => x.AwardRewardPointCreditsAsync(It.IsAny<int?>()), Times.Once);
        }

        protected class MockSqlDistributedLock : IDisposable
        {
            public void Dispose()
            {
                // Intentionally left blank
            }
        }
    }
}
