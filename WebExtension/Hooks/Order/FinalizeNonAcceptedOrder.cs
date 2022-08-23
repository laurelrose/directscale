using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Orders;
using System;
using System.Threading.Tasks;
using WebExtension.Services.ZiplingoEngagementService;

namespace WebExtension.Hooks.Order
{
    public class FinalizeNonAcceptedOrder : IHook<FinalizeNonAcceptedOrderHookRequest, FinalizeNonAcceptedOrderHookResponse>
    {
        private readonly IZiplingoEngagementService _ziplingoEngagementService;

        public FinalizeNonAcceptedOrder(IZiplingoEngagementService ziplingoEngagementService)
        {
            _ziplingoEngagementService = ziplingoEngagementService ?? throw new ArgumentNullException(nameof(ziplingoEngagementService));
        }
        public async Task<FinalizeNonAcceptedOrderHookResponse> Invoke(FinalizeNonAcceptedOrderHookRequest request, Func<FinalizeNonAcceptedOrderHookRequest, Task<FinalizeNonAcceptedOrderHookResponse>> func)
        {
            var result = await func(request);
            try
            {
                if (request.Order.OrderType == OrderType.Autoship)
                {
                    _ziplingoEngagementService.CallOrderZiplingoEngagementTrigger(request.Order, "AutoShipFailed", true);
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