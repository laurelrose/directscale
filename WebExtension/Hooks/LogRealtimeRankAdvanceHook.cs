using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Commissions;
using DirectScale.Disco.Extension.Services;
using System;
using System.Threading.Tasks;
using WebExtension.Repositories;
using ZiplingoEngagement.Services.Interface;

namespace WebExtension.Hooks
{
    public class LogRealtimeRankAdvanceHook : IHook<LogRealtimeRankAdvanceHookRequest, LogRealtimeRankAdvanceHookResponse>
    {
        private readonly ICustomLogRepository _customLogRepository;
        private readonly IAssociateService _associateService;
        private readonly IZLAssociateService _zlassociateService;
        private readonly IZLOrderZiplingoService _zlorderService;

        public LogRealtimeRankAdvanceHook(IAssociateService associateService, ICustomLogRepository customLogRepository, IZLAssociateService zlassociateService, IZLOrderZiplingoService zlorderService)
        {
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
            _associateService = associateService ?? throw new ArgumentNullException(nameof(associateService));
            _zlassociateService = zlassociateService ?? throw new ArgumentNullException(nameof(zlassociateService));
            _zlorderService = zlorderService ?? throw new ArgumentNullException(nameof(zlorderService));

        }
        public async Task<LogRealtimeRankAdvanceHookResponse> Invoke(LogRealtimeRankAdvanceHookRequest request, Func<LogRealtimeRankAdvanceHookRequest, Task<LogRealtimeRankAdvanceHookResponse>> func)
        {
            var result = await func(request);
            var associate = await _associateService.GetAssociate(request.AssociateId);
            try
            {
                _zlorderService.LogRealtimeRankAdvanceEvent(request);
                _zlassociateService.UpdateContact(associate);
            }
            catch (Exception ex)
            {
                _customLogRepository.CustomErrorLog(request.OldRank, request.NewRank, "", "Error : " + ex.Message);
            }
            return result;
        }
    }
}