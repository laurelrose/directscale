using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Services;
using Moq;
using NUnit.Framework;
using WebExtension.Services;
using WebExtension.Services.RewardPoints;
using WebExtension.Services.RewardPoints.Models;

namespace WebExtensionTests.Services.RewardPoints
{
    [TestFixture]
    internal abstract class RewardPointServiceTests
    {
        protected Mock<ICustomLogService> CustomLogServiceMock;
        protected Mock<IOrderService> OrderServiceMock;
        protected Mock<IRewardPointRepository> RewardPointRepositoryMock;
        protected Mock<IRewardPointsService> RewardPointsServiceMock;
        protected Mock<IStatsService> StatsServiceMock;

        protected RewardPointService RewardPointService;

        [SetUp]
        public void RewardPointServiceTestsSetUp()
        {
            CustomLogServiceMock = new Mock<ICustomLogService>();
            OrderServiceMock = new Mock<IOrderService>();
            RewardPointRepositoryMock = new Mock<IRewardPointRepository>();
            RewardPointsServiceMock = new Mock<IRewardPointsService>();
            StatsServiceMock = new Mock<IStatsService>();

            RewardPointService = new RewardPointService(
                CustomLogServiceMock.Object,
                OrderServiceMock.Object,
                RewardPointRepositoryMock.Object,
                RewardPointsServiceMock.Object,
                StatsServiceMock.Object
            );
        }
    }

    [TestFixture]
    internal class RewardPointServiceAwardRewardPointCreditsAsyncTests : RewardPointServiceTests
    {
        private const int AssociateId2 = 2;
        private const int AssociateId7 = 7;
        private const int AssociateId11 = 11;

        private CommissionPeriodInfo _commissionPeriodInfo;
        private Dictionary<int, List<RewardPointCredit>> _associateRewardPointCredits;
        private Dictionary<int, CommissionStats> _commissionStats;

        [SetUp]
        public void RewardPointServiceAwardRewardPointCreditsAsyncTestsSetUp()
        {
            _commissionPeriodInfo = new CommissionPeriodInfo
            {
                CommitDate = DateTime.Today.AddDays(-1),
                BeginDate = DateTime.Today.AddDays(-15),
                EndDate = DateTime.Today.AddDays(-9),
                CommissionPeriodId = 22
            };

            _associateRewardPointCredits = new Dictionary<int, List<RewardPointCredit>>
            {
                {
                    AssociateId2,
                    new List<RewardPointCredit> {
                        new() {
                            AwardedAssociateId = AssociateId2,
                            OrderNumber = 133,
                            PayoutStatus = PayoutStatus.Unpaid,
                            OrderAssociateId = 55,
                            CreditType = RewardPointCreditType.FirstTimeOrderPurchase,
                            Id = 1,
                            CommissionPeriodId = null,
                            OrderAssociateName = "Associate 55",
                            OrderItemId = 1,
                            OrderItemDescription = "Test Item 1",
                            OrderItemCredits = 20,
                            OrderCommissionDate = _commissionPeriodInfo.BeginDate.AddDays(1),
                            OrderItemSku = "T_I_1"
                        },
                        new()
                        {
                            AwardedAssociateId = AssociateId2,
                            OrderNumber = 136,
                            PayoutStatus = PayoutStatus.Unpaid,
                            OrderAssociateId = 56,
                            CreditType = RewardPointCreditType.FirstTimeOrderPurchase,
                            Id = 3,
                            CommissionPeriodId = null,
                            OrderAssociateName = "Associate 56",
                            OrderItemId = 1,
                            OrderItemDescription = "Test Item 1",
                            OrderItemCredits = 20,
                            OrderCommissionDate = _commissionPeriodInfo.BeginDate.AddDays(2),
                            OrderItemSku = "T_I_1"
                        },
                        new()
                        {
                            AwardedAssociateId = AssociateId2,
                            OrderNumber = 142,
                            PayoutStatus = PayoutStatus.Unpaid,
                            OrderAssociateId = 56,
                            CreditType = RewardPointCreditType.FirstTimeItemPurchase,
                            Id = 6,
                            CommissionPeriodId = null,
                            OrderAssociateName = "Associate 56",
                            OrderItemId = 3,
                            OrderItemDescription = "Test Item 3",
                            OrderItemCredits = 40,
                            OrderCommissionDate = _commissionPeriodInfo.BeginDate.AddDays(3),
                            OrderItemSku = "T_I_3"
                        }
                    }
                },
                {
                    AssociateId11,
                    new List<RewardPointCredit> {
                        new() {
                            AwardedAssociateId = AssociateId11,
                            OrderNumber = 200,
                            PayoutStatus = PayoutStatus.Unpaid,
                            OrderAssociateId = 99,
                            CreditType = RewardPointCreditType.FirstTimeItemPurchase,
                            Id = 2,
                            CommissionPeriodId = null,
                            OrderAssociateName = "Associate 99",
                            OrderItemId = 3,
                            OrderItemDescription = "Test Item 3",
                            OrderItemCredits = 20,
                            OrderCommissionDate = _commissionPeriodInfo.EndDate.AddDays(-2),
                            OrderItemSku = "T_I_3"
                        },
                        new()
                        {
                            AwardedAssociateId = AssociateId11,
                            OrderNumber = 217,
                            PayoutStatus = PayoutStatus.Unpaid,
                            OrderAssociateId = 101,
                            CreditType = RewardPointCreditType.FirstTimeItemPurchase,
                            Id = 4,
                            CommissionPeriodId = null,
                            OrderAssociateName = "Associate 101",
                            OrderItemId = 3,
                            OrderItemDescription = "Test Item 3",
                            OrderItemCredits = 20,
                            OrderCommissionDate = _commissionPeriodInfo.EndDate.AddDays(-2),
                            OrderItemSku = "T_I_3"
                        },
                        new()
                        {
                            AwardedAssociateId = AssociateId11,
                            OrderNumber = 554,
                            PayoutStatus = PayoutStatus.Unpaid,
                            OrderAssociateId = 113,
                            CreditType = RewardPointCreditType.FirstTimeItemPurchase,
                            Id = 5,
                            CommissionPeriodId = null,
                            OrderAssociateName = "Associate 113",
                            OrderItemId = 3,
                            OrderItemDescription = "Test Item 3",
                            OrderItemCredits = 20,
                            OrderCommissionDate = _commissionPeriodInfo.EndDate.AddDays(-1),
                            OrderItemSku = "T_I_3"
                        }
                    }
                }
            };

            _commissionStats = new Dictionary<int, CommissionStats>
            {
                { AssociateId2, new CommissionStats { AssociateID = AssociateId2, Kpis = new Dictionary<string, Kpi> { { "KIT", new Kpi { IsBool = true, Value = 1 } } } } },
                { AssociateId7, new CommissionStats { AssociateID = AssociateId7, Kpis = new Dictionary<string, Kpi> { { "KIT", new Kpi { IsBool = true, Value = 1 } } } } },
                { AssociateId11, new CommissionStats { AssociateID = AssociateId11, Kpis = new Dictionary<string, Kpi> { { "KIT", new Kpi { IsBool = true, Value = 0 } } } } }
            };

            RewardPointRepositoryMock
                .Setup(x => x.GetCurrentCommissionPeriodInfoAsync(It.IsAny<int?>()))
                .ReturnsAsync(_commissionPeriodInfo);

            RewardPointRepositoryMock
                .Setup(x => x.GetAssociateRewardPointCredits(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(_associateRewardPointCredits);

            RewardPointRepositoryMock.Setup(x => x.UpdateRewardPointCreditsAsync(It.IsAny<List<RewardPointCredit>>()));

            StatsServiceMock
                .Setup(x => x.GetStats(It.IsAny<int[]>(), It.IsAny<DateTime>()))
                .ReturnsAsync(_commissionStats);

            CustomLogServiceMock.Setup(x => x.SaveLog(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            RewardPointsServiceMock.Setup(x => x.AddRewardPointsWithExpiration(It.IsAny<int>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()));
        }

        [Test]
        public async Task RewardPointService_AwardRewardPointCreditsAsync_ExceptionThrown()
        {
            // Arrange
            const string error = "This is an error";

            RewardPointRepositoryMock
                .Setup(x => x.GetCurrentCommissionPeriodInfoAsync(It.IsAny<int?>()))
                .ThrowsAsync(new Exception(error));

            // Act
            await RewardPointService.AwardRewardPointCreditsAsync();

            // Assert
            RewardPointRepositoryMock.Verify(x => x.GetAssociateRewardPointCredits(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
            CustomLogServiceMock.Verify(x => x.SaveLog(0, 0, It.IsAny<string>(), "Error", error, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task RewardPointService_AwardRewardPointCreditsAsync_NullCommissionPeriodInfo()
        {
            // Arrange
            RewardPointRepositoryMock
                .Setup(x => x.GetCurrentCommissionPeriodInfoAsync(It.IsAny<int?>()))
                .ReturnsAsync((CommissionPeriodInfo)null);

            // Act
            await RewardPointService.AwardRewardPointCreditsAsync();

            // Assert
            RewardPointRepositoryMock.Verify(x => x.GetCurrentCommissionPeriodInfoAsync(It.IsAny<int?>()), Times.Once);
            RewardPointRepositoryMock.Verify(x => x.GetAssociateRewardPointCredits(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
            RewardPointRepositoryMock.Verify(x => x.UpdateRewardPointCreditsAsync(It.IsAny<List<RewardPointCredit>>()), Times.Never);
            CustomLogServiceMock.Verify(x => x.SaveLog(0, 0, It.IsAny<string>(), "Error", "Unable to retrieve Commission Period Info", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task RewardPointService_AwardRewardPointCreditsAsync_SkippingRewardPointAwardRun()
        {
            // Arrange
            _commissionPeriodInfo = new CommissionPeriodInfo
            {
                CommitDate = DateTime.Today,
                BeginDate = DateTime.Today.AddDays(-14),
                EndDate = DateTime.Today.AddDays(-8),
                CommissionPeriodId = 22
            };

            RewardPointRepositoryMock
                .Setup(x => x.GetCurrentCommissionPeriodInfoAsync(It.IsAny<int?>()))
                .ReturnsAsync(_commissionPeriodInfo);

            // Act
            await RewardPointService.AwardRewardPointCreditsAsync();

            // Assert
            RewardPointRepositoryMock.Verify(x => x.GetCurrentCommissionPeriodInfoAsync(It.IsAny<int?>()), Times.Once);
            RewardPointRepositoryMock.Verify(x => x.GetAssociateRewardPointCredits(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
            RewardPointRepositoryMock.Verify(x => x.UpdateRewardPointCreditsAsync(It.IsAny<List<RewardPointCredit>>()), Times.Never);
            CustomLogServiceMock.Verify(x => x.SaveLog(0, 0, It.IsAny<string>(), "Information", $"Skipping Reward Point Award Run: {DateTime.Today:d} is not at least 1 day after Commission Period {_commissionPeriodInfo.CommissionPeriodId}'s commit date ({_commissionPeriodInfo.CommitDate:d}).", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task RewardPointService_AwardRewardPointCreditsAsync_NoRewardPointCreditEntries()
        {
            // Arrange
            _associateRewardPointCredits.Remove(AssociateId2);
            _associateRewardPointCredits.Remove(AssociateId11);

            // Act
            await RewardPointService.AwardRewardPointCreditsAsync();

            // Assert
            RewardPointRepositoryMock.Verify(x => x.GetCurrentCommissionPeriodInfoAsync(It.IsAny<int?>()), Times.Once);
            RewardPointRepositoryMock.Verify(x => x.GetAssociateRewardPointCredits(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
            StatsServiceMock.Verify(x => x.GetStats(It.IsAny<int[]>(), It.IsAny<DateTime>()), Times.Once);
            RewardPointRepositoryMock.Verify(x => x.UpdateRewardPointCreditsAsync(It.IsAny<List<RewardPointCredit>>()), Times.Never);
            CustomLogServiceMock.Verify(x => x.SaveLog(0, 0, It.IsAny<string>(), "Information", "Terminating process - no Reward Point Credits to add.", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task RewardPointService_AwardRewardPointCreditsAsync_NoStatsEntries()
        {
            // Arrange
            _commissionStats.Remove(AssociateId2);
            _commissionStats.Remove(AssociateId7);
            _commissionStats.Remove(AssociateId11);

            // Act
            await RewardPointService.AwardRewardPointCreditsAsync();

            // Assert
            RewardPointRepositoryMock.Verify(x => x.GetCurrentCommissionPeriodInfoAsync(It.IsAny<int?>()), Times.Once);
            RewardPointRepositoryMock.Verify(x => x.GetAssociateRewardPointCredits(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
            StatsServiceMock.Verify(x => x.GetStats(It.IsAny<int[]>(), It.IsAny<DateTime>()), Times.Once);
            RewardPointRepositoryMock.Verify(x => x.UpdateRewardPointCreditsAsync(It.IsAny<List<RewardPointCredit>>()), Times.Never);
            CustomLogServiceMock.Verify(x => x.SaveLog(0, 0, It.IsAny<string>(), "Information", "Terminating process - no Reward Point Credits to add.", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task RewardPointService_AwardRewardPointCreditsAsync_ProcessEndedWithErrors()
        {
            // Arrange
            RewardPointsServiceMock
                .Setup(x => x.AddRewardPointsWithExpiration(It.IsAny<int>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<int?>()))
                .ThrowsAsync(new Exception("Oh no! It's an exception."));

            // Act
            await RewardPointService.AwardRewardPointCreditsAsync();

            // Assert
            RewardPointRepositoryMock.Verify(x => x.GetCurrentCommissionPeriodInfoAsync(It.IsAny<int?>()), Times.Once);
            RewardPointRepositoryMock.Verify(x => x.GetAssociateRewardPointCredits(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
            StatsServiceMock.Verify(x => x.GetStats(It.IsAny<int[]>(), It.IsAny<DateTime>()), Times.Once);
            RewardPointRepositoryMock.Verify(x => x.UpdateRewardPointCreditsAsync(It.IsAny<List<RewardPointCredit>>()), Times.Once);
            CustomLogServiceMock.Verify(x => x.SaveLog(0, 0, It.IsAny<string>(), "Information", "Process complete with errors.", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public async Task RewardPointService_AwardRewardPointCreditsAsync_ProcessEndedWithoutErrors()
        {
            // Act
            await RewardPointService.AwardRewardPointCreditsAsync();

            // Assert
            RewardPointRepositoryMock.Verify(x => x.GetCurrentCommissionPeriodInfoAsync(It.IsAny<int?>()), Times.Once);
            RewardPointRepositoryMock.Verify(x => x.GetAssociateRewardPointCredits(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Once);
            StatsServiceMock.Verify(x => x.GetStats(It.IsAny<int[]>(), It.IsAny<DateTime>()), Times.Once);
            RewardPointRepositoryMock.Verify(x => x.UpdateRewardPointCreditsAsync(It.IsAny<List<RewardPointCredit>>()), Times.Once);
            CustomLogServiceMock.Verify(x => x.SaveLog(0, 0, It.IsAny<string>(), "Information", "Process complete.", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
    }
}
