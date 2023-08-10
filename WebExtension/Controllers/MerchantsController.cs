using Microsoft.AspNetCore.Mvc;
using System;
using WebExtension.Helper;
using WebExtension.Repositories;

namespace WebExtension.Controllers
{
    [Route("api/[controller]")]
    public class MerchantsController : Controller
    {
        private readonly ICustomLogRepository _customLogRepository;

        public MerchantsController(ICustomLogRepository customLogRepository)
        {
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
        }

        //[HttpPost]
        //[Route("DbCopyAfter")]
        //public IActionResult DbCopyAfter()
        //{
        //    try
        //    {
        //        _ziplingoEngagementRepository.ResetSettings();
        //        return new Responses().OkResult(1);
        //    }
        //    catch (Exception e)
        //    {
        //        _customLogRepository.CustomErrorLog(0, 0, $"An error occurred with in DbCopy Function", $"Error :  {e.Message}");
        //        return new Responses().BadRequestResult(e.Message);
        //    }
        //}
    }
}
