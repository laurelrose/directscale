using DirectScale.Disco.Extension.Hooks.Orders;
using DirectScale.Disco.Extension.Hooks;
using System.Threading.Tasks;
using System;
using ZiplingoEngagement.Services.Interface;

namespace WebExtension.Hooks.Order
{
    public class FullRefundOrderHook : IHook<FullRefundOrderHookRequest, FullRefundOrderHookResponse>
    {

        private readonly IZLOrderZiplingoService _zlorderService;

        public FullRefundOrderHook(IZLOrderZiplingoService zlorderService)
        {
            _zlorderService = zlorderService ?? throw new ArgumentNullException(nameof(zlorderService));
        }
        public async Task<FullRefundOrderHookResponse> Invoke(FullRefundOrderHookRequest request, Func<FullRefundOrderHookRequest, Task<FullRefundOrderHookResponse>> func)
        {
            try
            {
                var response = await func(request);

                _zlorderService.CallFullRefundOrderZiplingoEngagementTrigger(request.Order, "FullRefundOrder", false);
                return response;
            }
            catch (Exception e)
            {

            }
            return new FullRefundOrderHookResponse();
        }
    }
}
