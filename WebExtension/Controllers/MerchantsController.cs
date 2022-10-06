using Microsoft.AspNetCore.Mvc;
using System;
using WebExtension.Helper;
using WebExtension.Repositories;
using WebExtension.Services.ZiplingoEngagementService;

namespace WebExtension.Controllers
{
    [Route("api/[controller]")]
    public class MerchantsController : Controller
    {
        private readonly IZiplingoEngagementRepository _ziplingoEngagementRepository;
        private readonly ICustomLogRepository _customLogRepository;

        public MerchantsController(IZiplingoEngagementRepository ziplingoEngagementRepository, ICustomLogRepository customLogRepository)
        {
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
            _ziplingoEngagementRepository = ziplingoEngagementRepository ?? throw new ArgumentNullException(nameof(ziplingoEngagementRepository));
        }

        [HttpPost]
        [Route("DbCopyAfter")]
        public IActionResult DbCopyAfter()
        {
            try
            {
                _ziplingoEngagementRepository.ResetSettings();
                return new Responses().OkResult(1);
            }
            catch (Exception e)
            {
                _customLogRepository.CustomErrorLog(0, 0, $"An error occurred with in DbCopy Function", $"Error :  {e.Message}");
                return new Responses().BadRequestResult(e.Message);
            }
        }
    }
}
