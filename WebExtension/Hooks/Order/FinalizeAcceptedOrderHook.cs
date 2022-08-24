using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Orders;
using DirectScale.Disco.Extension.Services;
using System;
using System.Threading.Tasks;
using WebExtension.Repositories;
using WebExtension.Services.ZiplingoEngagementService;

namespace WebExtension.Hooks.Order
{
    public class FinalizeAcceptedOrderHook : IHook<FinalizeAcceptedOrderHookRequest, FinalizeAcceptedOrderHookResponse>
    {
        private readonly IZiplingoEngagementService _ziplingoEngagementService;
        private readonly IOrderService _orderService;
        private readonly ICustomLogRepository _customLogRepository;

        public FinalizeAcceptedOrderHook(ICustomLogRepository customLogRepository, IZiplingoEngagementService ziplingoEngagementService,IOrderService orderService)
        {
            _ziplingoEngagementService = ziplingoEngagementService ?? throw new ArgumentNullException(nameof(ziplingoEngagementService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
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
            }
            catch (Exception ex)
            {
                //await _ziplingoEngagementService.SaveCustomLogs(request.Order.AssociateId, request.Order.OrderNumber,"", "Error : " + ex.Message);
            }
            return result;
        }
    }
}