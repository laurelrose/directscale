using DirectScale.Disco.Extension.EventModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using WebExtension.Helper;
using WebExtension.Services;

namespace WebExtension.WebHooks.Order
{
    public interface ICreateOrderEventWebHook
    {
        Task Fire(CreateOrderEvent request);
    }
    public class CreateOrderEventWebHook : ICreateOrderEventWebHook
    {
        private readonly ICustomLogService _customLogService;
        public CreateOrderEventWebHook(ICustomLogService customLogService)
        {
            _customLogService = customLogService ?? throw new ArgumentException(nameof(customLogService));
        }

        public async Task Fire(CreateOrderEvent request)
        {
            string methodName = CommonMethod.GetCurrentMethodName(MethodBase.GetCurrentMethod());
            try
            {
                // Implemented Submit Order Code Here
            }
            catch (Exception ex)
            {
                await _customLogService.SaveLog(0, 0, methodName, "Error", ex.Message, "", "", "", CommonMethod.Serialize(ex));
                throw;
            }
        }
    }
}
