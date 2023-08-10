using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Associates;
using DirectScale.Disco.Extension.Services;
using System;
using System.Threading.Tasks;
using WebExtension.Repositories;
using ZiplingoEngagement.Services.Interface;

namespace WebExtension.Hooks.Associate
{
    public class UpdateAssociateHook : IHook<UpdateAssociateHookRequest, UpdateAssociateHookResponse>
    {
        private readonly IAssociateService _associateService;
        private readonly ICustomLogRepository _customLogRepository;
        private readonly IZLAssociateService _zlassociateService;

        public UpdateAssociateHook(ICustomLogRepository customLogRepository, IAssociateService associateService, IZLAssociateService zlassociateService)
        {
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
            _zlassociateService = zlassociateService ?? throw new ArgumentNullException(nameof(zlassociateService));

        }

        public async Task<UpdateAssociateHookResponse> Invoke(UpdateAssociateHookRequest request, Func<UpdateAssociateHookRequest, Task<UpdateAssociateHookResponse>> func)
        {
            var result = func(request);
            try
            {
                if (request.OldAssociateInfo.AssociateBaseType != request.UpdatedAssociateInfo.AssociateBaseType)
                {
                    // Call AssociateTypeChange Trigger
                        var OldAssociateType = await _associateService.GetAssociateTypeName(request.OldAssociateInfo.AssociateBaseType);
                        var UpdatedAssociateType = await _associateService.GetAssociateTypeName(request.UpdatedAssociateInfo.AssociateBaseType);
                    _zlassociateService.AssociateTypeChange(request.UpdatedAssociateInfo.AssociateId, OldAssociateType, UpdatedAssociateType, request.UpdatedAssociateInfo.AssociateBaseType);                        
                }
                if (request.OldAssociateInfo.StatusId != request.UpdatedAssociateInfo.StatusId)
                {
                    _zlassociateService.AssociateStatusChange(request.UpdatedAssociateInfo.AssociateId, request.OldAssociateInfo.StatusId, request.UpdatedAssociateInfo.StatusId);
                }

                var associate1 = await _associateService.GetAssociate(request.UpdatedAssociateInfo.AssociateId);
                _zlassociateService.UpdateContact(associate1);
            }
            catch (Exception ex)
            {
                _customLogRepository.CustomErrorLog(request.OldAssociateInfo.AssociateBaseType, request.UpdatedAssociateInfo.AssociateBaseType, "", "Error : " + ex.Message);
            }
            
            return await result;
        }
    }
}
