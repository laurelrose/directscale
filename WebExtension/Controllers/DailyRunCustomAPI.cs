using DirectScale.Disco.Extension.EventModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebExtension.Helper;
using WebExtension.Hooks;
using WebExtension.Repositories;
using WebExtension.Services.DailyRun;
using WebExtension.Services.ZiplingoEngagementService;

namespace WebExtension.Controllers
{
    public class DailyRunCustomAPI : ControllerBase
    {
        private readonly IZiplingoEngagementService _ziplingoEngagementService;
        private readonly IDailyRunService _dailyRunService;
        private readonly ICustomLogRepository _customLogRepository;
        public DailyRunCustomAPI(IZiplingoEngagementService ziplingoEngagementService, IDailyRunService dailyRunService, ICustomLogRepository customLogRepository)
        {
  
                _ziplingoEngagementService = ziplingoEngagementService ?? throw new ArgumentNullException(nameof(ziplingoEngagementService));
                _dailyRunService = dailyRunService ?? throw new ArgumentNullException(nameof(dailyRunService));
                _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
            
        }

        [HttpPost]
        [Route("DailyRun_CustomApi")]
        public IActionResult DailyRunCustomAPi()
        {
            try
            {
                _ziplingoEngagementService.AssociateBirthDateTrigger();
                _ziplingoEngagementService.AssociateWorkAnniversaryTrigger();
                _dailyRunService.FiveDayRun();
                _dailyRunService.SentNotificationOnCardExpiryBefore30Days();
                _dailyRunService.ExecuteCommissionEarned();
                _dailyRunService.GetAssociateStatuses();

                return new Responses().OkResult();
            }
            catch (Exception ex)
            {
                return new Responses().ErrorResult(ex);
            }
        }
    }
}
