using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.Services;
using DirectScale.Disco.Extension.EventModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using WebExtension.Helper;
using WebExtension.Helper.Interface;
using WebExtension.Helper.Models;
using WebExtension.Services;
using System.Reflection;
using WebExtension.WebHooks.Order;

namespace WebExtension.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebHookController : ControllerBase
    {
        [ViewData]
        public string DSBaseUrl { get; set; }
        //
        private readonly ICustomLogService _customLogService;
        private readonly ICreateOrderEventWebHook _createOrderEventWebHook;
        public WebHookController(ICommonService commonService, ICustomLogService customLogService,
            ICreateOrderEventWebHook createOrderEventWebHook
        )
        {
            //
            _customLogService = customLogService ?? throw new ArgumentException(nameof(customLogService));
            _createOrderEventWebHook = createOrderEventWebHook ?? throw new ArgumentException(nameof(createOrderEventWebHook));
        }

        [HttpPost]
        [Route("CreateOrderEvent")]
        public async Task<IActionResult> CreateOrderEvent([FromBody] CreateOrderEvent request)
        {
            string methodName = CommonMethod.GetCurrentMethodName(MethodBase.GetCurrentMethod());
            try
            {
                if (request != null)
                {
                    await _customLogService.SaveLog(0, 0, methodName, "Info", "Web Hook Begin", "", "", $"{CommonMethod.Serialize(request)}", "");
                    await _createOrderEventWebHook.Fire(request);
                }
                else
                {
                    await _customLogService.SaveLog(0, 0, methodName, "Error", "WebHook Request not in JSON Format", "", "", $"{CommonMethod.Serialize(request)}", "");
                }

                return new Responses().OkResult();
            }
            catch (Exception ex)
            {
                await LogError(methodName, ex);
                return new Responses().BadRequestResult(ex.Message);
            }
        }

        private async Task LogError(string methodName, Exception ex)
        {
            await _customLogService.SaveLog(0, 0, methodName, "Error", ex.Message, "", "", "", CommonMethod.Serialize(ex));
        }

        /*[HttpPost]
        [Route("CreateOrderEvent")]
        public async Task<IActionResult> CreateOrderEvent()
        {
            string methodName = CommonMethod.GetCurrentMethodName(MethodBase.GetCurrentMethod());
            CreateAssociateEvent data = null;
            string body = string.Empty;
            try
            {
                (data, body) = await CommonMethod.ReadBodyFromContext<CreateAssociateEvent>(HttpContext);

                if (data != null)
                {
                    await _createOrderEventWebHook.Fire(data);
                }
                else
                {
                    await _customLogService.SaveLog(0, 0, methodName, "Error", "WebHook Request not in JSON Format", "", "", $"{body}", "");
                }

                return new Responses().OkResult();
            }
            catch (Exception ex)
            {
                await LogError(methodName, ex);
                return new Responses().BadRequestResult(ex.Message);
            }
        }*/
    }
}
