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
using WebExtension.Services.ZiplingoEngagementService;

namespace WebExtension.Hooks.Order
{
    public class FinalizeAcceptedOrderHook : IHook<FinalizeAcceptedOrderHookRequest, FinalizeAcceptedOrderHookResponse>
    {
        private readonly IZiplingoEngagementService _ziplingoEngagementService;
        private readonly IOrderService _orderService;
        private readonly ICustomLogRepository _customLogRepository;
        private readonly IOrderWebService _orderWebService;
        private readonly IRewardPointService _rewardPointService;

        public FinalizeAcceptedOrderHook(
            ICustomLogRepository customLogRepository,
            IZiplingoEngagementService ziplingoEngagementService,
            IOrderService orderService,
            IOrderWebService orderWebService,
            IRewardPointService rewardPointService
        )
        {
            _ziplingoEngagementService = ziplingoEngagementService ?? throw new ArgumentNullException(nameof(ziplingoEngagementService));
            _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
            _orderWebService = orderWebService ?? throw new ArgumentNullException(nameof(orderWebService));
            _rewardPointService = rewardPointService ?? throw new ArgumentNullException(nameof(rewardPointService));
        }

        public async Task<FinalizeAcceptedOrderHookResponse> Invoke(FinalizeAcceptedOrderHookRequest request, Func<FinalizeAcceptedOrderHookRequest, Task<FinalizeAcceptedOrderHookResponse>> func)
        {
            var result = await func(request);
            try
            {
                DirectScale.Disco.Extension.Order order = await _orderService.GetOrderByOrderNumber(request.Order.OrderNumber);
                if (order.OrderType == OrderType.Enrollment)
                {
                    _ziplingoEngagementService.CreateEnrollContact(order);
                }
  
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