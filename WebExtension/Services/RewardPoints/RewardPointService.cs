using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebExtension.Helper;
using WebExtension.Services.RewardPoints.Models;

namespace WebExtension.Services.RewardPoints
{
    public interface IRewardPointService
    {
        Task AwardRewardPointCreditsAsync(int? comPeriodId = null);
        Task SaveRewardPointCreditsAsync(Order order);
    }

    public class RewardPointService : IRewardPointService
    {
        private static readonly string _className = $"LaurelRose{nameof(RewardPointService)}";
        private const int DaysBetweenComRunAndPayout = 9;

        private readonly ICustomLogService _customLogService;
        private readonly IOrderService _orderService;
        private readonly IRewardPointRepository _rewardPointRepository;
        private readonly IRewardPointsService _rewardPointsService;
        private readonly IStatsService _statsService;

        public RewardPointService(
            ICustomLogService customLogService,
            IOrderService orderService,
            IRewardPointRepository rewardPointRepository,
            IRewardPointsService rewardPointsService,
            IStatsService statsService
        )
        {
            _customLogService = customLogService ?? throw new ArgumentNullException(nameof(customLogService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _rewardPointRepository = rewardPointRepository ?? throw new ArgumentNullException(nameof(rewardPointRepository));
            _rewardPointsService = rewardPointsService ?? throw new ArgumentNullException(nameof(rewardPointsService));
            _statsService = statsService ?? throw new ArgumentNullException(nameof(statsService));
        }

        public async Task AwardRewardPointCreditsAsync(int? comPeriodId = null)
        {
            try
            {
                var commissionPeriodInfo = await _rewardPointRepository.GetCurrentCommissionPeriodInfoAsync(comPeriodId);
                var dateToExecute = commissionPeriodInfo.CommitDate.Date.AddDays(DaysBetweenComRunAndPayout);
                if (DateTime.Today >= dateToExecute.Date)
                {
                    await _customLogService.SaveLog(0, 0, $"{_className}.AwardRewardPointCreditsAsync", "Information", $"Skipping Reward Point Award Run: {DateTime.Today:d} is not at least {DaysBetweenComRunAndPayout} days after Commission Period {commissionPeriodInfo.CommissionPeriodId}'s commit date ({commissionPeriodInfo.CommitDate:d}).", "", "", "", "");
                    return;
                }

                var rewardPointCreditsMap = await _rewardPointRepository.GetRewardPointCreditsByAwardedAssociateIdAsync(commissionPeriodInfo.BeginDate, commissionPeriodInfo.EndDate);
                var associateStatsMap = await _statsService.GetStats(rewardPointCreditsMap.Keys.ToArray(), commissionPeriodInfo.EndDate);
                var rewardPointCreditsToAdd = new List<RewardPointCredit>();
                foreach (var kvp in rewardPointCreditsMap)
                {
                    if (associateStatsMap.TryGetValue(kvp.Key, out var associateStats)
                        && associateStats != null
                        && associateStats.Kpis["KIT"].Value > 0)
                    {
                        rewardPointCreditsToAdd.AddRange(kvp.Value);
                    }
                }

                foreach (var rewardPointCredit in rewardPointCreditsToAdd)
                {
                    try
                    {
                        var descriptionString = $"Reward points earned from {rewardPointCredit.OrderAssociateName} Order {rewardPointCredit.OrderNumber}, Item {rewardPointCredit.OrderItemId} - '{rewardPointCredit.OrderItemDescription}'.";
                        await _rewardPointsService.AddRewardPointsWithExpiration(
                            rewardPointCredit.AwardedAssociateId,
                            rewardPointCredit.OrderItemCredits,
                            descriptionString,
                            DateTime.Today,
                            DateTime.Today.AddYears(1),
                            rewardPointCredit.OrderNumber
                        );

                        rewardPointCredit.CommissionPeriodId = commissionPeriodInfo.CommissionPeriodId;
                        rewardPointCredit.PayoutStatus = PayoutStatus.Paid;
                        await _customLogService.SaveLog(rewardPointCredit.AwardedAssociateId, rewardPointCredit.OrderNumber, $"{_className}.AwardRewardPointCreditsAsync", "Payout Information", descriptionString, "", "", "", CommonMethod.Serialize(rewardPointCredit));
                    }
                    catch (Exception e)
                    {
                        rewardPointCredit.PayoutStatus = PayoutStatus.Error;
                        await _customLogService.SaveLog(rewardPointCredit.AwardedAssociateId, rewardPointCredit.OrderNumber, $"{_className}.AwardRewardPointCreditsAsync", "Payout Error", e.Message, "", "", "", CommonMethod.Serialize(e));
                    }
                }

                await _rewardPointRepository.UpdateRewardPointCreditsAsync(rewardPointCreditsToAdd);
            }
            catch (Exception e)
            {
                await _customLogService.SaveLog(0, 0, $"{_className}.AwardRewardPointCreditsAsync", "Error", e.Message, "", "", "", CommonMethod.Serialize(e));
            }
        }

        public async Task SaveRewardPointCreditsAsync(Order order)
        {
            try
            {
                var repAssociateId = await GetRepAssociateIdAsync(order.AssociateId);
                if (order.AssociateId == repAssociateId)
                {
                    await _orderService.Log(order.OrderNumber, "RewardPoint Credits: No points awarded. A Rep cannot earn points for their own order.");
                    return;
                }

                var awardedOrderItemIds = new HashSet<int>();
                var rewardPointCredits = new List<RewardPointCredit>();
                if (TryGetFirstTimeOrderCredits(order, out var orderCreditMap))
                {
                    foreach (var orderLineItem in order.LineItems)
                    {
                        if (orderCreditMap.TryGetValue(orderLineItem.ItemId, out var orderCredit))
                        {
                            rewardPointCredits.Add(new RewardPointCredit
                            {
                                AwardedAssociateId = repAssociateId,
                                CreditType = RewardPointCreditType.FirstTimeOrderPurchase,
                                OrderAssociateId = order.AssociateId,
                                OrderAssociateName = order.Name,
                                OrderCommissionDate = order.CommissionDate,
                                OrderItemCredits = orderCredit,
                                OrderItemDescription = orderLineItem.ProductName,
                                OrderItemId = orderLineItem.ItemId,
                                OrderItemSku = orderLineItem.SKU,
                                OrderNumber = order.OrderNumber,
                                PayoutStatus = PayoutStatus.Unpaid
                            });

                            awardedOrderItemIds.Add(orderLineItem.ItemId);
                        }
                    }
                }

                var awardedItemItemIds = new HashSet<int>();
                if (TryGetFirstTimeItemCredits(order, awardedOrderItemIds, out var itemCreditMap))
                {
                    foreach (var orderLineItem in order.LineItems)
                    {
                        if (itemCreditMap.TryGetValue(orderLineItem.ItemId, out var itemCredit))
                        {
                            rewardPointCredits.Add(new RewardPointCredit
                            {
                                AwardedAssociateId = repAssociateId,
                                CreditType = RewardPointCreditType.FirstTimeItemPurchase,
                                OrderAssociateId = order.AssociateId,
                                OrderAssociateName = order.Name,
                                OrderCommissionDate = order.CommissionDate,
                                OrderItemCredits = itemCredit,
                                OrderItemDescription = orderLineItem.ProductName,
                                OrderItemId = orderLineItem.ItemId,
                                OrderItemSku = orderLineItem.SKU,
                                OrderNumber = order.OrderNumber,
                                PayoutStatus = PayoutStatus.Unpaid
                            });

                            awardedItemItemIds.Add(orderLineItem.ItemId);
                        }
                    }
                }

                switch (rewardPointCredits.Count)
                {
                    case 1:
                        await _rewardPointRepository.SaveRewardPointCreditAsync(rewardPointCredits.First());
                        break;
                    case > 1:
                        await _rewardPointRepository.SaveRewardPointCreditsAsync(rewardPointCredits);
                        break;
                }

                if (awardedOrderItemIds.Any())
                {
                    await _orderService.Log(order.OrderNumber, $"RewardPoint Credits: First-time Order credit awarded for the following item(s): {string.Join(", ", awardedOrderItemIds.ToArray())}.");
                }

                if (awardedItemItemIds.Any())
                {
                    await _orderService.Log(order.OrderNumber, $"RewardPoint Credits: First-time Item credit awarded for the following item(s): {string.Join(", ", awardedItemItemIds.ToArray())}.");
                }
            }
            catch (Exception e)
            {
                await _orderService.Log(order.OrderNumber, $"RewardPoint Credits: Error recording reward point credits: '{e.Message}'. Please review Custom Logs.");
                await _customLogService.SaveLog(order.AssociateId, order.OrderNumber, $"{_className}.SaveRewardPointCreditsAsync", "Error", e.Message, "", "", "", CommonMethod.Serialize(e));
            }
        }

        private async Task<int> GetRepAssociateIdAsync(int orderAssociateId)
        {
            var repId = await _rewardPointRepository.GetRepAssociateIdAsync(orderAssociateId);
            if (repId < 1)
            {
                throw new Exception($"Unable to find a Rep in upline for Order Associate {orderAssociateId}");
            }

            return repId;
        }

        private bool TryGetFirstTimeItemCredits(Order order, HashSet<int> awardedOrderItemIds, out Dictionary<int, double> itemCreditMap)
        {
            bool hasFirstTimeItems;
            if (order.Custom.Field1.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            {
                itemCreditMap = new Dictionary<int, double>();
                hasFirstTimeItems = false;
            }
            else
            {
                var itemIds = new HashSet<int>(order.LineItems.Select(x => x.ItemId));
                itemIds.ExceptWith(awardedOrderItemIds);

                var itemsIdsAlreadyDiscounted = _rewardPointRepository.GetFirstTimeItemPurchases(order.AssociateId, itemIds);
                itemIds.ExceptWith(itemsIdsAlreadyDiscounted);

                itemCreditMap = _rewardPointRepository.GetFirstTimeItemDiscounts(itemIds);
                hasFirstTimeItems = itemCreditMap.Any();
            }

            return hasFirstTimeItems;
        }

        private bool TryGetFirstTimeOrderCredits(Order order, out Dictionary<int, double> orderCreditMap)
        {
            bool isFirstTimeOrder;
            if (order.Custom.Field1.Equals("TRUE", StringComparison.OrdinalIgnoreCase) || _rewardPointRepository.GetFirstTimeOrderPurchaseCount(order.AssociateId) > 1)
            {
                orderCreditMap = new Dictionary<int, double>();
                isFirstTimeOrder = false;
            }
            else
            {
                var itemIds = new HashSet<int>(order.LineItems.Select(x => x.ItemId));
                orderCreditMap = _rewardPointRepository.GetFirstTimeOrderCredits(itemIds);
                isFirstTimeOrder = orderCreditMap.Any();
            }

            return isFirstTimeOrder;
        }
    }
}
