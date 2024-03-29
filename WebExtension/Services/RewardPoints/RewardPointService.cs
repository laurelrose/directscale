﻿using DirectScale.Disco.Extension;
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
        Task SaveRewardPointCreditsAsync(int orderNumber);
    }

    public class RewardPointService : IRewardPointService
    {
        private static readonly string _className = $"LaurelRose{nameof(RewardPointService)}";

        // Typically, LaurelRose commits and pays ont on the same day.
        // We are processing the RWD credits the day after for good measure.
        private const int DaysBetweenCommitAndPayout = 1;

        private readonly ICustomLogService _customLogService;
        private readonly IOrderService _orderService;
        private readonly IRewardPointRepository _rewardPointRepository;
        private readonly IRewardPointsService _rewardPointsService;
        private readonly IStatsService _statsService;
        private readonly ITreeService _treeService;

        public RewardPointService(
            ICustomLogService customLogService,
            IOrderService orderService,
            IRewardPointRepository rewardPointRepository,
            IRewardPointsService rewardPointsService,
            IStatsService statsService,
            ITreeService treeService
        )
        {
            _customLogService = customLogService ?? throw new ArgumentNullException(nameof(customLogService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _rewardPointRepository = rewardPointRepository ?? throw new ArgumentNullException(nameof(rewardPointRepository));
            _rewardPointsService = rewardPointsService ?? throw new ArgumentNullException(nameof(rewardPointsService));
            _statsService = statsService ?? throw new ArgumentNullException(nameof(statsService));
            _treeService = treeService ?? throw new ArgumentNullException(nameof(treeService));
        }

        public async Task AwardRewardPointCreditsAsync(int? comPeriodId = null)
        {
            try
            {
                var commissionPeriodInfo = await _rewardPointRepository.GetCurrentCommissionPeriodInfoAsync(comPeriodId);
                if (commissionPeriodInfo is null)
                {
                    throw new Exception("Unable to retrieve Commission Period Info");
                }

                // Business Requirement: The RWD credits payout shouldn't happen until one dat after the latest weekly commission period run.
                // Here, we are checking to see if today's date is one dat after the Commit Date of the most-recent commission period.
                var dateToExecute = commissionPeriodInfo.CommitDate.Date.AddDays(DaysBetweenCommitAndPayout);
                if (DateTime.Today < dateToExecute.Date)
                {
                    await _customLogService.SaveLog(0, 0, $"{_className}.AwardRewardPointCreditsAsync", "Information", $"Skipping Reward Point Award Run: {DateTime.Today:d} is not at least {DaysBetweenCommitAndPayout} day after Commission Period {commissionPeriodInfo.CommissionPeriodId}'s commit date ({commissionPeriodInfo.CommitDate:d}).", "", "", "", "");
                    return;
                }

                var rewardPointCreditsMap = await _rewardPointRepository.GetAssociateRewardPointCredits(commissionPeriodInfo.BeginDate, commissionPeriodInfo.EndDate);
                var associateStatsMap = await _statsService.GetStats(rewardPointCreditsMap.Keys.ToArray(), commissionPeriodInfo.EndDate);
                var rewardPointCreditsToAdd = new List<RewardPointCredit>();
                foreach (var kvp in rewardPointCreditsMap)
                {
                    if (associateStatsMap.TryGetValue(kvp.Key, out var associateStats) && associateStats != null)
                    {
                        // Business Requirement: In order for RWD credits to be awarded, Reps need to be "KIT Qualified".
                        // This means specifically that the "KIT" KPI needs to be set to true (1).
                        if (associateStats.Kpis.TryGetValue("KIT", out var kpi) && kpi != null)
                        {
                            if (kpi.Value > 0)
                            {
                                rewardPointCreditsToAdd.AddRange(kvp.Value);
                            }
                        }
                        else
                        {
                            // If the KPI isn't found, there is a larger problem, and we should throw an exception to end the process.
                            throw new Exception("KPI 'KIT' not found");
                        }
                    }
                }

                // Skip the rest of the code if there aren't any RWD credits to award.
                if (!rewardPointCreditsToAdd.Any())
                {
                    await _customLogService.SaveLog(0, 0, $"{_className}.AwardRewardPointCreditsAsync", "Information", "Terminating process - no Reward Point Credits to add.", "", "", "", "");
                    return;
                }

                foreach (var rewardPointCredit in rewardPointCreditsToAdd)
                {
                    try
                    {
                        // Business Requirement: The description indicates several things:
                        //    1. The Associate from whom the RWD credit was earned
                        //    2. The Order Number
                        //    3. The Item and quantity for which the credit was earned
                        var descriptionString = $"Reward points earned from {rewardPointCredit.OrderAssociateName} Order {rewardPointCredit.OrderNumber}, Item {rewardPointCredit.OrderItemSku} - '{rewardPointCredit.OrderItemDescription}', Qty: {rewardPointCredit.OrderItemQty:N}.";
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
                        await _orderService.Log(rewardPointCredit.OrderNumber, "RewardPoint Credits awarded successfully.");
                        await _customLogService.SaveLog(rewardPointCredit.AwardedAssociateId, rewardPointCredit.OrderNumber, $"{_className}.AwardRewardPointCreditsAsync", "Payout Information", descriptionString, "", "", "", CommonMethod.Serialize(rewardPointCredit));
                    }
                    catch (Exception e)
                    {
                        // Setting the PayoutStatus to "Error" if something went wrong.
                        // This ensures that the next RWD credit run will pick these credits up and attempt to award them.
                        rewardPointCredit.PayoutStatus = PayoutStatus.Error;
                        await _orderService.Log(rewardPointCredit.OrderNumber, "Error awarding RewardPoint Credits. Check custom logs for details.");
                        await _customLogService.SaveLog(rewardPointCredit.AwardedAssociateId, rewardPointCredit.OrderNumber, $"{_className}.AwardRewardPointCreditsAsync", "Payout Error", e.Message, "", "", "", CommonMethod.Serialize(e));
                    }
                }

                await _rewardPointRepository.UpdateRewardPointCreditsAsync(rewardPointCreditsToAdd);
                if (rewardPointCreditsToAdd.Any(x => x.PayoutStatus == PayoutStatus.Error))
                {
                    await _customLogService.SaveLog(0, 0, $"{_className}.AwardRewardPointCreditsAsync", "Information", "Process complete with errors.", "", "", "", "");
                }
                else
                {
                    await _customLogService.SaveLog(0, 0, $"{_className}.AwardRewardPointCreditsAsync", "Information", "Process complete.", "", "", "", "");
                }
            }
            catch (Exception e)
            {
                await _customLogService.SaveLog(0, 0, $"{_className}.AwardRewardPointCreditsAsync", "Error", e.Message, "", "", "", CommonMethod.Serialize(e));
            }
        }

        public async Task SaveRewardPointCreditsAsync(int orderNumber)
        {
            Order order = null;
            try
            {
                order = await _orderService.GetOrderByOrderNumber(orderNumber);

                // Business Requirement: The associate's enroller (whether a Customer or a Rep) will be the one receiving the reward points for the order.
                var repAssociateId = await GetRepAssociateIdAsync(order.AssociateId);
                
                var awardedOrderItemIds = new HashSet<int>();
                var rewardPointCredits = new List<RewardPointCredit>();
                var orderLogMessage = new List<string>();
                if (TryGetFirstTimeOrderCredits(order, out var orderCreditMap))
                {
                    foreach (var orderLineItem in order.LineItems)
                    {
                        if (orderCreditMap.TryGetValue(orderLineItem.ItemId, out var orderCredit) && orderCredit > 0)
                        {
                            var aggregateOrderCredit = orderCredit * orderLineItem.Qty;
                            rewardPointCredits.Add(new RewardPointCredit
                            {
                                AwardedAssociateId = repAssociateId,
                                CreditType = RewardPointCreditType.FirstTimeOrderPurchase,
                                OrderAssociateId = order.AssociateId,
                                OrderAssociateName = order.Name,
                                OrderCommissionDate = order.CommissionDate,
                                OrderItemCredits = aggregateOrderCredit,
                                OrderItemDescription = orderLineItem.ProductName,
                                OrderItemId = orderLineItem.ItemId,
                                OrderItemSku = orderLineItem.SKU,
                                OrderItemQty = orderLineItem.Qty,
                                OrderNumber = order.OrderNumber,
                                PayoutStatus = PayoutStatus.Unpaid
                            });

                            awardedOrderItemIds.Add(orderLineItem.ItemId);
                            orderLogMessage.Add($"{aggregateOrderCredit:N} {RewardPointCreditType.FirstTimeOrderPurchase} points awarded from item '{orderLineItem.SKU}', qty {orderLineItem.Qty:N}");
                        }
                    }
                }

                if (TryGetFirstTimeItemCredits(order, awardedOrderItemIds, out var itemCreditMap))
                {
                    foreach (var orderLineItem in order.LineItems)
                    {
                        if (itemCreditMap.TryGetValue(orderLineItem.ItemId, out var itemCredit) && itemCredit > 0)
                        {
                            var aggregateItemCredit = itemCredit * orderLineItem.Qty;
                            rewardPointCredits.Add(new RewardPointCredit
                            {
                                AwardedAssociateId = repAssociateId,
                                CreditType = RewardPointCreditType.FirstTimeItemPurchase,
                                OrderAssociateId = order.AssociateId,
                                OrderAssociateName = order.Name,
                                OrderCommissionDate = order.CommissionDate,
                                OrderItemCredits = aggregateItemCredit,
                                OrderItemDescription = orderLineItem.ProductName,
                                OrderItemId = orderLineItem.ItemId,
                                OrderItemSku = orderLineItem.SKU,
                                OrderItemQty = orderLineItem.Qty,
                                OrderNumber = order.OrderNumber,
                                PayoutStatus = PayoutStatus.Unpaid
                            });

                            orderLogMessage.Add($"{aggregateItemCredit:N} {RewardPointCreditType.FirstTimeItemPurchase} points awarded from item '{orderLineItem.SKU}', qty {orderLineItem.Qty:N}");
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

                if (orderLogMessage.Any())
                {
                    await _orderService.Log(order.OrderNumber, $"RewardPoint Credits: {string.Join(", ", orderLogMessage.ToArray())}.");
                }
            }
            catch (Exception e)
            {
                if (order != null)
                {
                    await _orderService.Log(order.OrderNumber, $"RewardPoint Credits: Error recording reward point credits: '{e.Message}'. Please review Custom Logs.");
                }

                await _customLogService.SaveLog(order?.AssociateId ?? 0, order?.OrderNumber ?? 0, $"{_className}.SaveRewardPointCreditsAsync", "Error", e.Message, "", "", "", CommonMethod.Serialize(e));
            }
        }

        private async Task<int> GetRepAssociateIdAsync(int orderAssociateId)
        {
            var nodeDetails = await _treeService.GetUplineIds(new NodeId(orderAssociateId), TreeType.Enrollment);
            var repId = await _rewardPointRepository.GetRepAssociateIdAsync(nodeDetails, orderAssociateId);
            if (repId < 1)
            {
                throw new Exception($"Unable to find a Rep in upline for Order Associate {orderAssociateId}");
            }

            return repId;
        }

        private bool TryGetFirstTimeItemCredits(Order order, HashSet<int> awardedOrderItemIds, out Dictionary<int, double> itemCreditMap)
        {
            bool hasFirstTimeItems;

            // Business Requirement: If Field1 of the Order's CustomFields is set to "true" or some variation of it, then this indicates an order that was already received.
            // RWD credits do not apply to such orders.
            if ("TRUE".Equals(order.Custom.Field1, StringComparison.OrdinalIgnoreCase) || "1".Equals(order.Custom.Field1, StringComparison.OrdinalIgnoreCase))
            {
                itemCreditMap = new Dictionary<int, double>();
                hasFirstTimeItems = false;
            }
            else
            {
                // Business Requirement: It doesn't matter if the given Associate has already ordered the item in question.
                // If the item has a first-time item credit, an Associate can get the reward as many times as they purchase the item.
                var itemIds = new HashSet<int>(order.LineItems.Select(x => x.ItemId));
                itemIds.ExceptWith(awardedOrderItemIds);

                itemCreditMap = _rewardPointRepository.GetFirstTimeItemCredits(itemIds);
                hasFirstTimeItems = itemCreditMap.Any();
            }

            return hasFirstTimeItems;
        }

        private bool TryGetFirstTimeOrderCredits(Order order, out Dictionary<int, double> orderCreditMap)
        {
            bool isFirstTimeOrder;

            // Business Requirement: If Field1 of the Order's CustomFields is set to "true" or some variation of it, then this indicates an order that was already received.
            // RWD credits do not apply to such orders.
            //
            // Business Requirement: If the given Associate has already placed an order, the First-time Order promotion has already been achieved.
            // An Associate can only achieve a First-time Order once.
            if ("TRUE".Equals(order.Custom.Field1, StringComparison.OrdinalIgnoreCase) || "1".Equals(order.Custom.Field1, StringComparison.OrdinalIgnoreCase) || _rewardPointRepository.GetFirstTimeOrderPurchaseCount(order.AssociateId) > 1)
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
