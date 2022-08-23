using System;
using System.Linq;
using WebExtension.Repositories;
using WebExtension.Services.ZiplingoEngagementService;

namespace WebExtension.Services.DailyRun
{
    public interface IDailyRunService
    {
        void FiveDayRun();
        void SentNotificationOnCardExpiryBefore30Days();
        void ExecuteCommissionEarned();
    }

    public class DailyRunService : IDailyRunService
    {
        private readonly IDailyRunRepository _dailyRunRepository;
        private readonly IZiplingoEngagementService _ziplingoEngagementService;

        public DailyRunService(
            IDailyRunRepository dailyRunRepository,
            IZiplingoEngagementService ziplingoEngagementService)
        {
            _dailyRunRepository = dailyRunRepository ?? throw new ArgumentNullException(nameof(dailyRunRepository));
            _ziplingoEngagementService = ziplingoEngagementService ?? throw new ArgumentNullException(nameof(ziplingoEngagementService));
        }

        public  void FiveDayRun()
        {
            var autoships =  _dailyRunRepository.GetNextFiveDayAutoships();
            if (autoships.Count() > 0)
            {
                _ziplingoEngagementService.FiveDayRunTrigger(autoships);
            }
        }

        public void SentNotificationOnCardExpiryBefore30Days()
        {
            var expiryCreditCardInfoBefore30Days =  _dailyRunRepository.GetCreditCardInfoBefore30Days();
            if (expiryCreditCardInfoBefore30Days.Count() > 0)
            {
                _ziplingoEngagementService.ExpirationCardTrigger(expiryCreditCardInfoBefore30Days);
            }
        }

        public void ExecuteCommissionEarned()
        {
            _ziplingoEngagementService.ExecuteCommissionEarned();
        }
    }
}
