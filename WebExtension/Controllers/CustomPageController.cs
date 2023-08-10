using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Middleware;
using DirectScale.Disco.Extension.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebExtension.Helper.Interface;
using WebExtension.Helper.Models;
using WebExtension.Models;
using ZiplingoEngagement.Models.Settings;
using ZiplingoEngagement.Services.Interface;

namespace WebExtension.Controllers
{
    public class resObj
    {
        public ZiplingoEngagementSettings settings { get; set; }
        public List<ZiplingoEventSettings> eventSettings { get; set; }
    }
    public class CustomPageController : Controller
    {
        ////private configSetting _config;

        ////[ViewData]
        ////public string DSBaseUrl { get; set; }
        //////
        ////private readonly ICurrentUser _currentUser;
        ////private readonly ISettingsService _settingsService;
        //
        private readonly ICommonService _commonService;
        private readonly IZLSettingsService _zLSettingsService;
        public CustomPageController(ICommonService commonService, IZLSettingsService zLSettingsService)
        {
            
            _commonService = commonService ?? throw new ArgumentNullException(nameof(commonService));
            _zLSettingsService = zLSettingsService ?? throw new ArgumentNullException(nameof(zLSettingsService));
          
        }
        public IActionResult ZiplingoEngagementSetting()
        {
            ZiplingoEngagementSettings _settings = _zLSettingsService.GetSettings();
            List<ZiplingoEventSettings> _eventSettings = _zLSettingsService.GetEventSettingsList().GetAwaiter().GetResult();
            resObj viewDataSend = new resObj() { settings = _settings, eventSettings = _eventSettings };
            ViewBag.Message = viewDataSend;
            return View();
        }
        [ExtensionAuthorize]
        public async Task<IActionResult> CustomOrderReport()
        {
            return View();
        }
        
    }
}
