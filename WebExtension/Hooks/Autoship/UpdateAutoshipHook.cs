using DirectScale.Disco.Extension.Hooks.Autoships;
using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Services;
using System;
using WebExtension.Services.ZiplingoEngagementService;
using WebExtension.Services;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace WebExtension.Hooks.Autoship
{
    public class UpdateAutoshipHook : IHook<UpdateAutoshipHookRequest, UpdateAutoshipHookResponse>
    {
        private readonly ICustomLogService _customLogService;
        private readonly IZiplingoEngagementService _ziplingoEngagementService;
        private readonly IAutoshipService _autoshipService;
        private readonly IAssociateService _associateService;
        public UpdateAutoshipHook(ICustomLogService customLogService, IZiplingoEngagementService ziplingoEngagementService, IAutoshipService autoshipService, IAssociateService associateService)
        {
            _customLogService = customLogService ?? throw new ArgumentNullException(nameof(customLogService));
            _ziplingoEngagementService = ziplingoEngagementService ?? throw new ArgumentNullException(nameof(ziplingoEngagementService));
            _autoshipService = autoshipService ?? throw new ArgumentNullException(nameof(autoshipService));
            _associateService = associateService;
        }

        public async Task<UpdateAutoshipHookResponse> Invoke(UpdateAutoshipHookRequest request, Func<UpdateAutoshipHookRequest, Task<UpdateAutoshipHookResponse>> func)
        {
            //Set ShipAddress null for bypass create autoship shipping address validation. it will took ShipAddress from associate detail after bypass validation.
            try
            {
                request.AutoshipInfo.ShipAddress = null;
            }
            catch (Exception ex)
            {
                await _customLogService.SaveLog(request.AutoshipInfo.AssociateId, 0, "UpdateAutoshipHook : Before", "Error", ex.Message, "", "", JsonConvert.SerializeObject(request), JsonConvert.SerializeObject(ex));
            }

            var response = await func(request);

            try
            {
                var updatedAutoshipInfo = await _autoshipService.GetAutoship(request.AutoshipInfo.AutoshipId);
                _ziplingoEngagementService.UpdateAutoshipTrigger(updatedAutoshipInfo);
                var associateSummary = await _associateService.GetAssociate(request.AutoshipInfo.AssociateId);
                _ziplingoEngagementService.UpdateContact(associateSummary);
            }
            catch (Exception ex)
            {

            }

            return response;
        }
    }
}
