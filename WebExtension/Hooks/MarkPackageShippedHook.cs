using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Orders.Packages;
using System;
using System.Threading.Tasks;
using ZiplingoEngagement.Services.Interface;

namespace WebExtension.Hooks
{
    public class MarkPackageShippedHook : IHook<MarkPackagesShippedHookRequest, MarkPackagesShippedHookResponse>
    {
        private readonly IZLOrderZiplingoService _zlorderService;

        public MarkPackageShippedHook( IZLOrderZiplingoService zlorderService)
        {
            _zlorderService = zlorderService ?? throw new ArgumentNullException(nameof(zlorderService));
        }
        public async Task<MarkPackagesShippedHookResponse> Invoke(MarkPackagesShippedHookRequest request, Func<MarkPackagesShippedHookRequest, Task<MarkPackagesShippedHookResponse>> func)
        {
            var result = await func(request);
            try
            {
                foreach (var shipInfo in request.PackageStatusUpdates)
                {
                    _zlorderService.SendOrderShippedEmail(shipInfo.PackageId, shipInfo.TrackingNumber);
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