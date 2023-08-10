using DirectScale.Disco.Extension.EventModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebExtension.Helper;
using WebExtension.Hooks;
using WebExtension.Repositories;
using WebExtension.Services.DailyRun;
using ZiplingoEngagement.Services.Interface;

namespace WebExtension.Controllers
{
    public class DailyRunCustomAPI : ControllerBase
    {
        private readonly IZLAssociateService _zlassociateService;
        private readonly IDailyRunService _dailyRunService;
        private readonly ICustomLogRepository _customLogRepository;
        public DailyRunCustomAPI( IDailyRunService dailyRunService, ICustomLogRepository customLogRepository, IZLAssociateService zlassociateService)
        {
                _dailyRunService = dailyRunService ?? throw new ArgumentNullException(nameof(dailyRunService));
                _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
            _zlassociateService = zlassociateService ?? throw new ArgumentNullException(nameof(zlassociateService));

        }

        [HttpPost]
        [Route("DailyRun_CustomApi")]
        public IActionResult DailyRunCustomAPi()
        {
            try
            {
                _zlassociateService.AssociateBirthDay();
                _zlassociateService.AssociateWorkAnniversary();
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
