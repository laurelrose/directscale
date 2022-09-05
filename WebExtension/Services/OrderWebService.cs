﻿using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebExtension.Hooks.Order;
using WebExtension.Models;
using WebExtension.Repositories;

namespace WebExtension.Services
{
    public interface IOrderWebService
    {
        Task<List<OrderViewModel>> GetFilteredOrders(string search, DateTime beginDate, DateTime endDate);
        Task<List<string>> GetKitLevelFiveSkuList();
        Task<CommissionStats> GetCustomerStats(int associateId);
        Task<Kpi> GetKpi(int associateId, string kpiName);
        void FinalizeAcceptedOrderAfter(DirectScale.Disco.Extension.Order order);
    }
    public class OrderWebService : IOrderWebService
    {
        const int PointExpirationDays = 90;
        private readonly IOrderWebRepository _orderWebRepository;
        private readonly IOrderService _orderService;
        private readonly ICurrencyService _currencyService;
        private readonly IStatsService _statsService;
        private readonly IAssociateService _associateService;
        private readonly IRewardPointsService _rewardPointsService;
        private readonly ITicketService _ticketService;

        public OrderWebService(IOrderWebRepository orderWebRepository,
            IOrderService orderService, ICurrencyService currencyService, IStatsService statsService, IAssociateService associateService,
            IRewardPointsService rewardPointsService,
            ITicketService ticketService)
        {
            _orderWebRepository = orderWebRepository ?? throw new ArgumentNullException(nameof(orderWebRepository));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
            _statsService = statsService ?? throw new ArgumentNullException(nameof(statsService));
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
            _rewardPointsService = rewardPointsService ?? throw new ArgumentNullException(nameof(rewardPointsService));
            _ticketService = ticketService ?? throw new ArgumentNullException(nameof(ticketService));
        }

        public async Task<List<OrderViewModel>> GetFilteredOrders(string search, DateTime beginDate, DateTime endDate)
        {
            try
            {
                var orderIds = _orderWebRepository.GetFilteredOrderIds(search, beginDate, endDate);
                if (orderIds.Count > 0)
                {
                    var orders = await _orderService.GetOrders(orderIds.ToArray());

                    return orders.Select(o =>
                    {
                        return new OrderViewModel(o)
                        {
                            USDTotalFormatted = o.USDTotal.ToString(),
                            USDSubTotalFormatted = o.USDSubTotal.ToString()
                        };
                    }).ToList();
                }
            }
            catch (Exception e)
            {

            }
            return new List<OrderViewModel>();
        }

        public async Task<List<string>> GetKitLevelFiveSkuList()
        {
            try
            {
                return _orderWebRepository.GetKitLevelFiveSkuList();
            }
            catch (Exception e)
            {

                return new List<string>();
            }
        }

        public async Task<CommissionStats> GetCustomerStats(int associateId)
        {
            CommissionStats stats = null;
            try
            {
                var statsList = await _statsService.GetStats(new int[] { associateId }, DateTime.Now);
                stats = statsList[associateId];
            }
            catch (Exception e)
            {

            }
            return stats;
        }

        public async Task<Kpi> GetKpi(int associateId, string kpiName)
        {
            Kpi kpi = null;
            try
            {
                var stats = await GetCustomerStats(associateId);
                kpi = stats.Kpis[kpiName];
            }
            catch (Exception e)
            {

            }
            return kpi;
        }

        // Save And Share ------------------------------------------------------------------

        public void FinalizeAcceptedOrderAfter(DirectScale.Disco.Extension.Order order)
        {
            if (order == null) return;
            ConsumeUsedPoints(order);
            DistributeShareAndSaveRewards(order);
        }

        private void ConsumeUsedPoints(DirectScale.Disco.Extension.Order order)
        {
            try
            {
                var orderCoupon = _orderWebRepository.GetCouponUsageByOrderId(order.OrderNumber).FirstOrDefault(x => x.CouponId == ProcessCouponCodesHook.ShareAndSaveCouponId);

                if (orderCoupon == null) return;

                _rewardPointsService.AddRewardPoints(order.AssociateId, -orderCoupon.Amount, string.Empty, order.OrderNumber);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private void DistributeShareAndSaveRewards(DirectScale.Disco.Extension.Order order)
        {
            try
            {
                var associateInfo = _associateService.GetAssociate(order.AssociateId);
                if (associateInfo.Result.AssociateBaseType!=2)
                {
                    return;
                }
                var sponsorId = _orderWebRepository.GetEnrollmentSponsorId(order.AssociateId);

                if (sponsorId == 0)
                {
                    return;
                }

                var sponsorInfo = _associateService.GetAssociate(sponsorId);

                if (sponsorInfo.Result.AssociateBaseType==2)
                {
                    var pointsBalance = Convert.ToDecimal(_rewardPointsService.GetRewardPoints(sponsorId));

                    var pointsToAward = (decimal)Math.Round((order.Totals.FirstOrDefault().SubTotal - order.Totals.FirstOrDefault().DiscountTotal) * .25, 2);

                    if (pointsToAward == 0)
                    {
                        return;
                    }

                    _rewardPointsService.AddRewardPointsWithExpiration(sponsorId,
                        (double)pointsToAward,
                        ProcessCouponCodesHook.ShareAndSave,
                        DateTime.Now,
                        DateTime.Now.AddDays(PointExpirationDays),
                        order.OrderNumber);

                    _ticketService.LogEvent(sponsorId,
                        $"Current RWD account balance {pointsBalance} RWD, " +
                        $"Order {order.OrderNumber} from {associateInfo.Result.Name} earned {pointsToAward} RWD. " +
                        $"New RWD account balance {pointsBalance + pointsToAward} RWD. " +
                        $"This distribution will expire in {PointExpirationDays} days.","","");

                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
