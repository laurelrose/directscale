using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Hooks.Order
{
    public class SubmitOrderHook : IHook<SubmitOrderHookRequest, SubmitOrderHookResponse>
    {
        public SubmitOrderHook()
        {
        }
        public Task<SubmitOrderHookResponse> Invoke(SubmitOrderHookRequest request, Func<SubmitOrderHookRequest, Task<SubmitOrderHookResponse>> func)
        {
            var result = func(request);
            return result;
        }
    }
}
