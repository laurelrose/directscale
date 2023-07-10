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
using WebExtension.Services.ZiplingoEngagementService;
using WebExtension.Services.ZiplingoEngagementService.Model;

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
        private readonly IZiplingoEngagementRepository _ziplingoEngagementRepository;
        public CustomPageController(ICommonService commonService, IZiplingoEngagementRepository ziplingoEngagementRepository)
        {
            
            _commonService = commonService ?? throw new ArgumentNullException(nameof(commonService));
            _ziplingoEngagementRepository= ziplingoEngagementRepository ?? throw new ArgumentNullException(nameof(ziplingoEngagementRepository));
          
        }
        public IActionResult ZiplingoEngagementSetting()
        {
            ZiplingoEngagementSettings _settings = _ziplingoEngagementRepository.GetSettings();
            List<ZiplingoEventSettings> _eventSettings = _ziplingoEngagementRepository.GetEventSettingsList();
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
