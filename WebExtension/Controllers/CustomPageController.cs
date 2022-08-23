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

namespace WebExtension.Controllers
{
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
        //
        public CustomPageController(ICommonService commonService)
        {
            ////_config = config.Value;
            //////
            ////_currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            ////_settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            //////
            ////DirectScale.Disco.Extension.ExtensionContext extension = _settingsService.ExtensionContext().Result;
            ////DSBaseUrl = _config.BaseURL.Replace("{clientId}", extension.ClientId).Replace("{environment}", extension.EnvironmentType == EnvironmentType.Live ? "" : "stage");
            //
            _commonService = commonService ?? throw new ArgumentNullException(nameof(commonService));
            //
        }

        [ExtensionAuthorize]
        public async Task<IActionResult> CustomOrderReport()
        {
            return View();
        }
    }
}
