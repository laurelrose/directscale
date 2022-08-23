using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Autoships;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebExtension.Services.ZiplingoEngagementService;

namespace WebExtension.Hooks.Autoship
{
    public class CreateAutoshipHook : IHook<CreateAutoshipHookRequest, CreateAutoshipHookResponse>
    {
        private readonly IZiplingoEngagementService _ziplingoEngagementService;

        public CreateAutoshipHook(IZiplingoEngagementService ziplingoEngagementService)
        {
            _ziplingoEngagementService = ziplingoEngagementService ?? throw new ArgumentNullException(nameof(ziplingoEngagementService));
        }
        public async Task<CreateAutoshipHookResponse> Invoke(CreateAutoshipHookRequest request, Func<CreateAutoshipHookRequest, Task<CreateAutoshipHookResponse>> func)
        {
            var response = await func(request);

            _ziplingoEngagementService.CreateAutoshipTrigger(request.AutoshipInfo);

            return response;
        }
    }
}
