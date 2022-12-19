using DirectScale.Disco.Extension.EventModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebExtension.Helper;
using WebExtension.Services;
using WebExtension.Services.RewardPoints;

namespace WebExtension.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebHookController : ControllerBase
    {
        public static string _className = nameof(WebHookController);

        private readonly ICustomLogService _customLogService;
        private readonly IRewardPointService _rewardPointService;

        public WebHookController(
            ICustomLogService customLogService,
            IRewardPointService rewardPointService
        )
        {
            _customLogService = customLogService ?? throw new ArgumentException(nameof(customLogService));
            _rewardPointService = rewardPointService ?? throw new ArgumentException(nameof(rewardPointService));
        }

        //[HttpPost]
        //[Route("CreateOrderEvent")]
        //public async Task<IActionResult> CreateOrderEvent([FromBody] CreateOrderEvent request)
        //{
        //    string methodName = CommonMethod.GetCurrentMethodName(MethodBase.GetCurrentMethod());
        //    try
        //    {
        //        if (request != null)
        //        {
        //            await _customLogService.SaveLog(0, 0, methodName, "Info", "Web Hook Begin", "", "", $"{CommonMethod.Serialize(request)}", "");
        //            await _createOrderEventWebHook.Fire(request);
        //        }
        //        else
        //        {
        //            await _customLogService.SaveLog(0, 0, methodName, "Error", "WebHook Request not in JSON Format", "", "", $"{CommonMethod.Serialize(request)}", "");
        //        }

        //        return new Responses().OkResult();
        //    }
        //    catch (Exception ex)
        //    {
        //        await LogErrorAsync(methodName, ex);
        //        return new Responses().BadRequestResult(ex.Message);
        //    }
        //}

        [HttpPost]
        [Route("DailyEvent")]
        public async Task<IActionResult> DailyEvent([FromBody] DailyEvent request)
        {
            try
            {
                await _customLogService.SaveLog(0, 0, $"{_className}.DailyEvent", "Information", "DailyEvent Triggered", "", "", "", CommonMethod.Serialize(request));

                await _rewardPointService.AwardRewardPointCreditsAsync();

                return new Responses().OkResult();
            }
            catch (Exception ex)
            {
                await LogErrorAsync($"{_className}.DailyEvent", ex);
                return new Responses().ErrorResult(ex);
            }
        }

        private async Task LogErrorAsync(string methodName, Exception ex)
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
                await LogErrorAsync(methodName, ex);
                return new Responses().BadRequestResult(ex.Message);
            }
        }*/
    }
}
