using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Orders.Packages;
using System;
using System.Threading.Tasks;
using WebExtension.Services.ZiplingoEngagementService;

namespace WebExtension.Hooks
{
    public class MarkPackageShippedHook : IHook<MarkPackagesShippedHookRequest, MarkPackagesShippedHookResponse>
    {
        private readonly IZiplingoEngagementService _ziplingoEngagementService;

        public MarkPackageShippedHook(IZiplingoEngagementService ziplingoEngagementService)
        {
            _ziplingoEngagementService = ziplingoEngagementService ?? throw new ArgumentNullException(nameof(ziplingoEngagementService));
        }
        public async Task<MarkPackagesShippedHookResponse> Invoke(MarkPackagesShippedHookRequest request, Func<MarkPackagesShippedHookRequest, Task<MarkPackagesShippedHookResponse>> func)
        {
            var result = await func(request);
            try
            {
                foreach (var shipInfo in request.PackageStatusUpdates)
                {
                    _ziplingoEngagementService.SendOrderShippedEmail(shipInfo.PackageId, shipInfo.TrackingNumber);
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