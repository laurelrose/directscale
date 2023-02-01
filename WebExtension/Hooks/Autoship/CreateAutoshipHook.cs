using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Autoships;
using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebExtension.Services.ZiplingoEngagementService;

namespace WebExtension.Hooks.Autoship
{
    public class CreateAutoshipHook : IHook<CreateAutoshipHookRequest, CreateAutoshipHookResponse>
    {
        private readonly IZiplingoEngagementService _ziplingoEngagementService;
        private readonly IAssociateService _associateService;
        private readonly IAutoshipService _autoshipService;

        public CreateAutoshipHook(IZiplingoEngagementService ziplingoEngagementService, IAssociateService associateService, IAutoshipService autoshipService)
        {
            _ziplingoEngagementService = ziplingoEngagementService ?? throw new ArgumentNullException(nameof(ziplingoEngagementService));
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
            _autoshipService = autoshipService ?? throw new ArgumentNullException(nameof(autoshipService));
        }
        public async Task<CreateAutoshipHookResponse> Invoke(CreateAutoshipHookRequest request, Func<CreateAutoshipHookRequest, Task<CreateAutoshipHookResponse>> func)
        {
            var response = await func(request);

            try 
            {
                var autoshipInfo = await _autoshipService.GetAutoship(response.AutoshipId);
                _ziplingoEngagementService.CreateAutoshipTrigger(autoshipInfo);
                var associateSummary = await _associateService.GetAssociate(autoshipInfo.AssociateId);
                _ziplingoEngagementService.UpdateContact(associateSummary);
            }
            catch(Exception ex)
            {

            } 
            return response;
        }
    }
}
