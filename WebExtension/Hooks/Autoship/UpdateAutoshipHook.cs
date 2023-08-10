using DirectScale.Disco.Extension.Hooks.Autoships;
using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Services;
using System;
using WebExtension.Services;
using Newtonsoft.Json;
using System.Threading.Tasks;
using ZiplingoEngagement.Services.Interface;

namespace WebExtension.Hooks.Autoship
{
    public class UpdateAutoshipHook : IHook<UpdateAutoshipHookRequest, UpdateAutoshipHookResponse>
    {
        private readonly ICustomLogService _customLogService;
        private readonly IAutoshipService _autoshipService;
        private readonly IAssociateService _associateService;
        private readonly IZLAssociateService _zlassociateService;
        private readonly IZLOrderZiplingoService _zlorderService;
        public UpdateAutoshipHook(ICustomLogService customLogService, IAutoshipService autoshipService, IAssociateService associateService, IZLAssociateService zlassociateService, IZLOrderZiplingoService zlorderService)
        {
            _customLogService = customLogService ?? throw new ArgumentNullException(nameof(customLogService));
            _autoshipService = autoshipService ?? throw new ArgumentNullException(nameof(autoshipService));
            _associateService = associateService;
            _zlassociateService = zlassociateService ?? throw new ArgumentNullException(nameof(zlassociateService));
            _zlorderService = zlorderService ?? throw new ArgumentNullException(nameof(zlorderService));
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
                _zlorderService.UpdateAutoship(updatedAutoshipInfo);
                var associateSummary = await _associateService.GetAssociate(request.AutoshipInfo.AssociateId);
                _zlassociateService.UpdateContact(associateSummary);
            }
            catch (Exception ex)
            {

            }

            return response;
        }
    }
}
