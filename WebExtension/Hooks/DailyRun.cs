using DirectScale.Disco.Extension.Middleware;
using Microsoft.AspNetCore.Mvc;
using System;
using WebExtension.Repositories;
using WebExtension.Services.DailyRun;
using WebExtension.Services.ZiplingoEngagementService;

namespace WebExtension.Hooks
{
    [Route("api/webhooks/Autoships")]
    [ApiController]
    public class DailyRunWebHookController : ControllerBase
    {
        private readonly IZiplingoEngagementService _ziplingoEngagementService;
        private readonly IDailyRunService _dailyRunService;
        private readonly ICustomLogRepository _customLogRepository;

        public DailyRunWebHookController(IZiplingoEngagementService ziplingoEngagementService, IDailyRunService dailyRunService, ICustomLogRepository customLogRepository)
        {
            _ziplingoEngagementService = ziplingoEngagementService ?? throw new ArgumentNullException(nameof(ziplingoEngagementService));
            _dailyRunService = dailyRunService ?? throw new ArgumentNullException(nameof(dailyRunService));
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
        }

        [ExtensionAuthorize]
        [HttpPost("DailyRun")]
        public IActionResult DailyRunWebHook()
        {
            try
            {
                _ziplingoEngagementService.AssociateBirthDateTrigger();
                _ziplingoEngagementService.AssociateWorkAnniversaryTrigger();
                _dailyRunService.FiveDayRun();
                _dailyRunService.SentNotificationOnCardExpiryBefore30Days();
                _dailyRunService.ExecuteCommissionEarned();
                _dailyRunService.GetAssociateStatuses(); //New sync api for associate statuses
                _ziplingoEngagementService.SentNotificationOnServiceExpiryBefore2Weeks();
            }
            catch (Exception ex)
            {
                _customLogRepository.CustomErrorLog(0, 0, "Error with in daily run hook", ex.Message);
            }

            return Ok();
        }
    }
}