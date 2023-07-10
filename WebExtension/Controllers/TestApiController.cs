using DirectScale.Disco.Extension.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using WebExtension.Services.DailyRun;
using WebExtension.Services.ZiplingoEngagementService;
using WebExtension.Services.ZiplingoEngagementService.Model;

namespace WebExtension.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestApiController : Controller
    {
        private readonly IDailyRunService _dailyRunService;
        private readonly IZiplingoEngagementRepository _ziplingoEngagementRepository;

        public TestApiController(IDailyRunService dailyRunService, IZiplingoEngagementRepository ziplingoEngagementRepository)
        {
            _dailyRunService = dailyRunService ?? throw new ArgumentNullException(nameof(dailyRunService)); 
            _ziplingoEngagementRepository= ziplingoEngagementRepository ?? throw new ArgumentNullException(nameof(ziplingoEngagementRepository));
        }

        [HttpGet]
        [Route("TestApi")]
        public IActionResult TestApi()
        {
            _dailyRunService.GetAssociateStatuses();
            return Ok();
        }
        [HttpPost]
        [Route("UpdateZiplingoEventSettings")]
        public IActionResult UpdateZiplingoEventSettings(ZiplingoEventSettingRequest request)
        {
            try
            {
                _ziplingoEngagementRepository.UpdateEventSetting(request);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
        [HttpPost]
        [Route("UpdateZiplingoEngagementSettings")]
        public IActionResult UpdateZiplingoEngagementSettings(ZiplingoEngagementSettings request)
        {
            try
            {
                _ziplingoEngagementRepository.UpdateSettings(request);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
        [HttpGet]
        [Route("GetZiplingoEventSettings")]
        public IActionResult GetZiplingoEventSettings()
        {
            try
            {
               var res= _ziplingoEngagementRepository.GetEventSettingsList();
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
    }
}
