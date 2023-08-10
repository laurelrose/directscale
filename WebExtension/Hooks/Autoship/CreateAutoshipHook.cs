using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Autoships;
using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZiplingoEngagement.Services.Interface;

namespace WebExtension.Hooks.Autoship
{
    public class CreateAutoshipHook : IHook<CreateAutoshipHookRequest, CreateAutoshipHookResponse>
    {
        private readonly IAssociateService _associateService;
        private readonly IAutoshipService _autoshipService;
        private readonly IZLAssociateService _zlassociateService;
        private readonly IZLOrderZiplingoService _zlorderService;

        public CreateAutoshipHook( IAssociateService associateService, IAutoshipService autoshipService, IZLAssociateService zlassociateService, IZLOrderZiplingoService zlorderService)
        {
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
            _autoshipService = autoshipService ?? throw new ArgumentNullException(nameof(autoshipService));
            _zlassociateService = zlassociateService ?? throw new ArgumentNullException(nameof(zlassociateService));
            _zlorderService = zlorderService ?? throw new ArgumentNullException(nameof(zlorderService));
        }
        public async Task<CreateAutoshipHookResponse> Invoke(CreateAutoshipHookRequest request, Func<CreateAutoshipHookRequest, Task<CreateAutoshipHookResponse>> func)
        {
            var response = await func(request);

            try 
            {
                var autoshipInfo = await _autoshipService.GetAutoship(response.AutoshipId);
                _zlorderService.CreateAutoship(autoshipInfo);
                var associateSummary = await _associateService.GetAssociate(autoshipInfo.AssociateId);
                _zlassociateService.UpdateContact(associateSummary);
            }
            catch(Exception ex)
            {

            } 
            return response;
        }
    }
}
