using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Orders;
using System;
using System.Threading.Tasks;
using ZiplingoEngagement.Services.Interface;

namespace WebExtension.Hooks.Order
{
    public class FinalizeNonAcceptedOrder : IHook<FinalizeNonAcceptedOrderHookRequest, FinalizeNonAcceptedOrderHookResponse>
    {

        private readonly IZLOrderZiplingoService _zlorderService;

        public FinalizeNonAcceptedOrder(IZLOrderZiplingoService zlorderService)
        {
            _zlorderService = zlorderService ?? throw new ArgumentNullException(nameof(zlorderService));
        }
        public async Task<FinalizeNonAcceptedOrderHookResponse> Invoke(FinalizeNonAcceptedOrderHookRequest request, Func<FinalizeNonAcceptedOrderHookRequest, Task<FinalizeNonAcceptedOrderHookResponse>> func)
        {
            var result = await func(request);
            try
            {
                if (request.Order.OrderType == OrderType.Autoship)
                {
                    _zlorderService.CallOrderZiplingoEngagement(request.Order, "AutoShipFailed", true);
                }
                else
                {
                    _zlorderService.CallOrderZiplingoEngagement(request.Order, "OrderFailed", true);
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