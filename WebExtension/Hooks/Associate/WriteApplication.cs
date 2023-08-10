using DirectScale.Disco.Extension.Hooks;
using DirectScale.Disco.Extension.Hooks.Associates.Enrollment;
using System;
using DirectScale.Disco.Extension;
using System.Threading.Tasks;
using ZiplingoEngagement.Services.Interface;

namespace WebExtension.Hooks.Associate
{
    public class WriteApplication : IHook<WriteApplicationHookRequest, WriteApplicationHookResponse>
    {
        private readonly IZLAssociateService _zlassociateService;

        public WriteApplication( IZLAssociateService zlassociateService)
        {
            _zlassociateService = zlassociateService ?? throw new ArgumentNullException(nameof(zlassociateService));
        }

        public Task<WriteApplicationHookResponse> Invoke(WriteApplicationHookRequest request, Func<WriteApplicationHookRequest, Task<WriteApplicationHookResponse>> func)
        {
            var response = func(request);
            try
            {
                _zlassociateService.CreateContact(request.Application, response.Result.ApplicationResponse);
                return response;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}