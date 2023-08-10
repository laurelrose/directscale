using Azure.Core;
using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebExtension.Hooks.Order;
using WebExtension.Models;
using WebExtension.Repositories;
using ZiplingoEngagement.Services.Interface;

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
        private readonly ICustomLogRepository _customLogRepository;
        private readonly ITreeService _treeService;
        private readonly IZLOrderZiplingoService _zlorderService;

        public OrderWebService(IOrderWebRepository orderWebRepository,
            IOrderService orderService, ICurrencyService currencyService, IStatsService statsService, IAssociateService associateService,
            IRewardPointsService rewardPointsService,
            ITicketService ticketService,
            ICustomLogRepository customLogRepository, ITreeService treeService, IZLOrderZiplingoService zlorderService)
        {
            _orderWebRepository = orderWebRepository ?? throw new ArgumentNullException(nameof(orderWebRepository));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _currencyService = currencyService ?? throw new ArgumentNullException(nameof(currencyService));
            _statsService = statsService ?? throw new ArgumentNullException(nameof(statsService));
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
            _rewardPointsService = rewardPointsService ?? throw new ArgumentNullException(nameof(rewardPointsService));
            _ticketService = ticketService ?? throw new ArgumentNullException(nameof(ticketService));
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
            _treeService = treeService ?? throw new ArgumentNullException(nameof(treeService));
            _zlorderService = zlorderService ?? throw new ArgumentNullException(nameof(zlorderService));
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
                if (stats != null) 
                {
                    kpi = stats.Kpis[kpiName];
                }
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
                _customLogRepository.CustomErrorLog(order.AssociateId, order.OrderNumber, "Error in ConsumeUsedPoints", "Error : " + ex.Message);
                throw new Exception(ex.Message);
            }
        }

        private async Task<int> DistributeShareAndSaveRewards(DirectScale.Disco.Extension.Order order)
        {
            try
            {
                var associateInfo = await _associateService.GetAssociate(order.AssociateId);
                if (associateInfo.AssociateType != 1)
                {
                    var sponsorId = _treeService.GetNodeDetail(new NodeId(order.AssociateId, 0), TreeType.Enrollment).Result?.UplineId.AssociateId ?? 0;

                    if (sponsorId != 0)
                    {
                        var sponsorInfo = await _associateService.GetAssociate(sponsorId);

                        if (sponsorInfo.AssociateType == 2 || sponsorInfo.AssociateType == 3)
                        {
                            var pointsBalance = await _rewardPointsService.GetRewardPoints(sponsorId);
                            var point = (decimal)Math.Round(pointsBalance, 2);

                            var pointsToAward = (decimal)Math.Round((order.Totals.FirstOrDefault().SubTotal - order.Totals.FirstOrDefault().DiscountTotal) * .25, 2);

                            if (pointsToAward != 0)
                            {
                                ProcessCouponCodesHook.ShareAndSave += $"{associateInfo.DisplayFirstName} {associateInfo.DisplayLastName}";

                                var r = await _rewardPointsService.AddRewardPointsWithExpiration(sponsorId,
                                   (double)pointsToAward,
                                   ProcessCouponCodesHook.ShareAndSave,
                                   DateTime.Now,
                                   DateTime.Now.AddDays(PointExpirationDays),
                                   order.OrderNumber);
                                if (r != null && sponsorId != 0)
                                {
                                    _zlorderService.CallOrderZiplingoEngagement(order, "RewardPointEarned", false); //Reward point variable true

                                    var t = await _ticketService.LogEvent(sponsorId,
                                    $"Current RWD account balance {point} RWD, " +
                                    $"Order {order.OrderNumber} from {associateInfo.Name} earned {pointsToAward} RWD. " +
                                    $"New RWD account balance {point + pointsToAward} RWD. " +
                                    $"This distribution will expire in {PointExpirationDays} days.", "", "");

                                    return 0;
                                }
                                else
                                {
                                    return 1;
                                }

                            }
                            else
                            {
                                return 1;
                            }
                        }
                        else
                        {
                            return 1;
                        }
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    return 1;
                }
            }
            catch (Exception ex)
            {
                _customLogRepository.CustomErrorLog(order.AssociateId, order.OrderNumber, "Error in DistributeShareAndSaveRewards", "Error : " + ex.Message);
                //throw new Exception(ex.Message);
                return 1;
            }
        }
    }
}
