using DirectScale.Disco.Extension.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using WebExtension.Services.DailyRun;
using ZiplingoEngagement.Models.Request;
using ZiplingoEngagement.Services.Interface;

namespace WebExtension.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestApiController : Controller
    {
        private readonly IDailyRunService _dailyRunService;
        private readonly IZLSettingsService _zLSettingsService;

        public TestApiController(IDailyRunService dailyRunService, IZLSettingsService zLSettingsService)
        {
            _dailyRunService = dailyRunService ?? throw new ArgumentNullException(nameof(dailyRunService));
            _zLSettingsService = zLSettingsService ?? throw new ArgumentNullException(nameof(zLSettingsService));
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
        public IActionResult UpdateZiplingoEventSettings(ZiplingoEventSettingsRequest request)
        {
            try
            {
                _zLSettingsService.UpdateEventSetting(request);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
        [HttpPost]
        [Route("UpdateZiplingoEngagementSettings")]
        public IActionResult UpdateZiplingoEngagementSettings(ZiplingoEngagementSettingsRequest request)
        {
            try
            {
                _zLSettingsService.UpdateSettings(request);
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
               var res= _zLSettingsService.GetEventSettingsList();
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
    }
}
