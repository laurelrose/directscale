﻿using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Orders;
using DirectScale.Disco.Extension.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebExtension.Repositories;
using WebExtension.Services;
using WebExtension.Services.ZiplingoEngagementService;

namespace WebExtension.Hooks.Order
{
    public class FinalizeAcceptedOrderHook : IHook<FinalizeAcceptedOrderHookRequest, FinalizeAcceptedOrderHookResponse>
    {
        private readonly IZiplingoEngagementService _ziplingoEngagementService;
        private readonly IOrderService _orderService;
        private readonly ICustomLogRepository _customLogRepository;
        private readonly IOrderWebService _orderWebService;
        

        public FinalizeAcceptedOrderHook(ICustomLogRepository customLogRepository, IZiplingoEngagementService ziplingoEngagementService,IOrderService orderService, IOrderWebService orderWebService)
        {
            _ziplingoEngagementService = ziplingoEngagementService ?? throw new ArgumentNullException(nameof(ziplingoEngagementService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
            _orderWebService = orderWebService ?? throw new ArgumentNullException(nameof(orderWebService));
        }
        public async Task<FinalizeAcceptedOrderHookResponse> Invoke(FinalizeAcceptedOrderHookRequest request, Func<FinalizeAcceptedOrderHookRequest, Task<FinalizeAcceptedOrderHookResponse>> func)
        {
            var result = await func(request);
            try
            {
                DirectScale.Disco.Extension.Order order = await _orderService.GetOrderByOrderNumber(request.Order.OrderNumber);
                if (order.Status == OrderStatus.Paid || order.IsPaid)
                {
                    var totalOrders = _orderService.GetOrdersByAssociateId(request.Order.AssociateId, "").Result;
                    if (totalOrders.Length == 1)
                    {
                        _ziplingoEngagementService.CallOrderZiplingoEngagementTrigger(order, "FirstOrderCreated", false);
                        _ziplingoEngagementService.CallOrderZiplingoEngagementTrigger(order, "OrderCreated", false);
                    }
                    else 
                    {
                        _ziplingoEngagementService.CallOrderZiplingoEngagementTrigger(order, "OrderCreated", false);
                    }
                }
                if (order.OrderType == OrderType.Autoship && (order.Status == OrderStatus.Declined || order.Status == OrderStatus.FraudRejected))
                {
                    _ziplingoEngagementService.CallOrderZiplingoEngagementTrigger(order, "AutoShipFailed", true);
                }
                //
                #region #3160 Trigger for Laurel Rose for Infinity Bottles Earned
                var orderItemSkuList = request.Order.LineItems.Select(x => x.SKU).ToList();
                var kitLevelFiveSkuList = await _orderWebService.GetKitLevelFiveSkuList();
                var KIT_Kpi = await _orderWebService.GetKpi(request.Order.AssociateId, "KIT");
                if (KIT_Kpi != null && KIT_Kpi.Value == 0 && kitLevelFiveSkuList.Any(x => orderItemSkuList.Any(y => y == x)))
                {
                    _ziplingoEngagementService.CallOrderZiplingoEngagementTrigger(order, "InfinityBottlesEarned", true);
                }
                #endregion
            }
            catch (Exception ex)
            {
                //await _ziplingoEngagementService.SaveCustomLogs(request.Order.AssociateId, request.Order.OrderNumber,"", "Error : " + ex.Message);
            }
            return result;
        }
    }
}