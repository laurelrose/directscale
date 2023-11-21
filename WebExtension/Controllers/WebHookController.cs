using Azure.Core;
using DirectScale.Disco.Extension;
using DirectScale.Disco.Extension.EventModels;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebExtension.Helper;
using WebExtension.Services;
using WebExtension.Services.DistributedLocking;
using WebExtension.Services.RewardPoints;

namespace WebExtension.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebHookController : ControllerBase
    {
        public static string _className = nameof(WebHookController);

        private readonly ICustomLogService _customLogService;
        private readonly IDistributedLockingService _distributedLockingService;
        private readonly IRewardPointService _rewardPointService;

        public WebHookController(
            ICustomLogService customLogService,
            IDistributedLockingService distributedLockingService,
            IRewardPointService rewardPointService
        )
        {
            _customLogService = customLogService ?? throw new ArgumentException(nameof(customLogService));
            _distributedLockingService = distributedLockingService ?? throw new ArgumentException(nameof(distributedLockingService));
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
                using (var @lock = await _distributedLockingService.CreateDistributedLockAsync("LaurelRose_AwardRewardPointCredits"))
                {
                    // If the lock is null, that means something else is using it.
                    // If it's already in use, ignore the request.
                    if (@lock != null)
                    {
                        await _customLogService.SaveLog(0, 0, $"{_className}.DailyEvent", "Information", "DailyEvent Triggered", "", "", "", CommonMethod.Serialize(request));
                        await _rewardPointService.AwardRewardPointCreditsAsync();
                    }
                }

                return new Responses().OkResult();
            }
            catch (Exception ex)
            {
                await LogErrorAsync($"{_className}.DailyEvent", ex);
                return new Responses().ErrorResult(ex);
            }
        }
        [HttpPost]
        [Route("CustomRewardsEvent")]
        public async Task<IActionResult> CustomRewardsEvent([FromBody] int request)
        {
            try
            {
                using (var @lock = await _distributedLockingService.CreateDistributedLockAsync("LaurelRose_AwardRewardPointCredits"))
                {
                    // If the lock is null, that means something else is using it.
                    // If it's already in use, ignore the request.
                    if (@lock != null)
                    {
                        await _customLogService.SaveLog(0, 0, $"{_className}.CustomEvent", "Information", "Custom Event Triggered", "", "", "", CommonMethod.Serialize(request));
                        await _rewardPointService.AwardRewardPointCreditsAsync(request);
                    }
                }

                return new Responses().OkResult();
            }
            catch (Exception ex)
            {
                await LogErrorAsync($"{_className}.CustomEvent", ex);
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
