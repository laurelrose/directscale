using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Orders;
using DirectScale.Disco.Extension.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebExtension.Repositories;
using WebExtension.Services;
using WebExtension.Services.RewardPoints;
using ZiplingoEngagement.Services.Interface;

namespace WebExtension.Hooks.Order
{
    public class FinalizeAcceptedOrderHook : IHook<FinalizeAcceptedOrderHookRequest, FinalizeAcceptedOrderHookResponse>
    {
        private readonly IOrderService _orderService;
        private readonly ICustomLogRepository _customLogRepository;
        private readonly IOrderWebService _orderWebService;
        private readonly IRewardPointService _rewardPointService;
        private readonly IZLAssociateService _zlassociateService;
        private readonly IZLOrderZiplingoService _zlorderService;

        public FinalizeAcceptedOrderHook(
            ICustomLogRepository customLogRepository,
            IOrderService orderService,
            IOrderWebService orderWebService,
            IRewardPointService rewardPointService,
            IZLOrderZiplingoService zlorderService,
             IZLAssociateService zlassociateService
        )
        {
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
            _orderWebService = orderWebService ?? throw new ArgumentNullException(nameof(orderWebService));
            _rewardPointService = rewardPointService ?? throw new ArgumentNullException(nameof(rewardPointService));
            _zlorderService = zlorderService ?? throw new ArgumentNullException(nameof(zlorderService));
            _zlassociateService = zlassociateService ?? throw new ArgumentNullException(nameof(zlassociateService));
        }

        public async Task<FinalizeAcceptedOrderHookResponse> Invoke(FinalizeAcceptedOrderHookRequest request, Func<FinalizeAcceptedOrderHookRequest, Task<FinalizeAcceptedOrderHookResponse>> func)
        {
            var result = await func(request);
            try
            {
                DirectScale.Disco.Extension.Order order = await _orderService.GetOrderByOrderNumber(request.Order.OrderNumber);
                if (order.OrderType == OrderType.Enrollment)
                {
                    _zlassociateService.CreateEnrollContact(order);
                }

                if (order.OrderType == OrderType.Autoship && (order.Status == OrderStatus.Declined || order.Status == OrderStatus.FraudRejected))
                {
                    _zlorderService.CallOrderZiplingoEngagement(order, "AutoShipFailed", true);
                }
                if (order.Status == OrderStatus.Paid || order.IsPaid)
                {
                    var totalOrders = _orderService.GetOrdersByAssociateId(order.AssociateId, "").Result;
                    if (totalOrders.Length == 1)
                    {
                        _zlorderService.CallOrderZiplingoEngagement(order, "FirstOrderCreated", false);
                        _zlorderService.CallOrderZiplingoEngagement(order, "OrderCreated", false);

                        //
                        #region #3159 Trigger for Reward Point Earned for Laurel Rose
                        //_ziplingoEngagementService.CallOrderZiplingoEngagementTrigger(order, "RewardPointEarned", false);
                        #endregion
                    }
                    else
                    {
                        _zlorderService.CallOrderZiplingoEngagement(order, "OrderCreated", false);
                        //
                        #region #3159 Trigger for Reward Point Earned for Laurel Rose
                        //_ziplingoEngagementService.CallOrderZiplingoEngagementTrigger(order, "RewardPointEarned", false);
                        #endregion
                    }
                }

                //
                #region #3160 Trigger for Laurel Rose for Infinity Bottles Earned
                var orderItemSkuList = order.LineItems.Select(x => x.SKU).ToList();
                var kitLevelFiveSkuList = await _orderWebService.GetKitLevelFiveSkuList();
                var KIT_Kpi = await _orderWebService.GetKpi(order.AssociateId, "KIT");
                if (KIT_Kpi != null && KIT_Kpi.Value == 0 && kitLevelFiveSkuList.Any(x => orderItemSkuList.Any(y => y == x)))
                {
                    _zlorderService.CallOrderZiplingoEngagement(order, "InfinityBottlesEarned", true);
                }
                #endregion

                _orderWebService.FinalizeAcceptedOrderAfter(request.Order);

            }
            catch (Exception ex)
            {
                //await _ziplingoEngagementService.SaveCustomLogs(request.Order.AssociateId, request.Order.OrderNumber,"", "Error : " + ex.Message);
            }

            await _rewardPointService.SaveRewardPointCreditsAsync(request.Order.OrderNumber);

            return result;
        }
    }
}