using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Associates;
using DirectScale.Disco.Extension.Services;
using System;
using System.Threading.Tasks;
using WebExtension.Repositories;
using WebExtension.Services.ZiplingoEngagementService;

namespace WebExtension.Hooks.Associate
{
    public class UpdateAssociateHook : IHook<UpdateAssociateHookRequest, UpdateAssociateHookResponse>
    {
        private readonly IZiplingoEngagementService _ziplingoEngagementService;
        private readonly IAssociateService _associateService;
        private readonly ICustomLogRepository _customLogRepository;

        public UpdateAssociateHook(ICustomLogRepository customLogRepository, IZiplingoEngagementService ziplingoEngagementService, IAssociateService associateService)
        {
            _ziplingoEngagementService = ziplingoEngagementService ?? throw new ArgumentNullException(nameof(ziplingoEngagementService));
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));

        }

        public async Task<UpdateAssociateHookResponse> Invoke(UpdateAssociateHookRequest request, Func<UpdateAssociateHookRequest, Task<UpdateAssociateHookResponse>> func)
        {
            var result = func(request);
            try
            {
                if (request.OldAssociateInfo.AssociateBaseType != request.UpdatedAssociateInfo.AssociateBaseType)
                {
                    // Call AssociateTypeChange Trigger
                    if (request.OldAssociateInfo.AssociateBaseType > 0 && request.UpdatedAssociateInfo.AssociateBaseType > 0)
                    {
                        var OldAssociateType = await _associateService.GetAssociateTypeName(request.OldAssociateInfo.AssociateBaseType);
                        var UpdatedAssociateType = await _associateService.GetAssociateTypeName(request.UpdatedAssociateInfo.AssociateBaseType);
                        _ziplingoEngagementService.UpdateAssociateType(request.UpdatedAssociateInfo.AssociateId, OldAssociateType, UpdatedAssociateType, request.UpdatedAssociateInfo.AssociateBaseType);                        
                    }
                }
                if (request.OldAssociateInfo.StatusId != request.UpdatedAssociateInfo.StatusId)
                {
                    _ziplingoEngagementService.AssociateStatusChangeTrigger(request.UpdatedAssociateInfo.AssociateId, request.OldAssociateInfo.StatusId, request.UpdatedAssociateInfo.StatusId);
                }

                var associate1 = await _associateService.GetAssociate(request.UpdatedAssociateInfo.AssociateId);
                _ziplingoEngagementService.UpdateContact(associate1);
            }
            catch (Exception ex)
            {
                _customLogRepository.CustomErrorLog(request.OldAssociateInfo.AssociateBaseType, request.UpdatedAssociateInfo.AssociateBaseType, "", "Error : " + ex.Message);
            }
            
            return await result;
        }
    }
}
