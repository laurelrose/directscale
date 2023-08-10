using System;
using System.Linq;
using WebExtension.Repositories;
using ZiplingoEngagement.Services.Interface;

namespace WebExtension.Services.DailyRun
{
    public interface IDailyRunService
    {
        void FiveDayRun();
        void SentNotificationOnCardExpiryBefore30Days();
        void ExecuteCommissionEarned();
        void GetAssociateStatuses();
    }

    public class DailyRunService : IDailyRunService
    {
        private readonly IDailyRunRepository _dailyRunRepository;
        private readonly IZLAssociateService _zlassociateService;

        public DailyRunService(
            IDailyRunRepository dailyRunRepository,IZLAssociateService zlassociateService)
        {
            _dailyRunRepository = dailyRunRepository ?? throw new ArgumentNullException(nameof(dailyRunRepository));
            _zlassociateService = zlassociateService ?? throw new ArgumentNullException(nameof(zlassociateService));
        }

        public  void FiveDayRun()
        {
            var autoships =  _dailyRunRepository.GetNextFiveDayAutoships();
            if (autoships.Count() > 0)
            {
                _zlassociateService.FiveDayRun(autoships);
            }
        }

        public void SentNotificationOnCardExpiryBefore30Days()
        {
            var expiryCreditCardInfoBefore30Days =  _dailyRunRepository.GetCreditCardInfoBefore30Days();
            if (expiryCreditCardInfoBefore30Days.Count() > 0)
            {
                _zlassociateService.ExpirationCard(expiryCreditCardInfoBefore30Days);
            }
        }

        public void ExecuteCommissionEarned()
        {
            _zlassociateService.ExecuteCommissionEarned();
        }

        public void GetAssociateStatuses()
        {
            var associateStatuses = _dailyRunRepository.GetAssociateStatuses();
            _zlassociateService.AssociateStatusSync(associateStatuses);
        }
    }
}
