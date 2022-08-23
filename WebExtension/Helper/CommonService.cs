using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebExtension.Helper.Interface;
using WebExtension.Helper.Models;

namespace WebExtension.Helper
{
    public class CommonService : ICommonService
    {
        public async Task<RequestBasePara> GetCallRequestPara(HttpContext context)
        {
            var request = new RequestBasePara();
            try
            {
                foreach (var queryKvp in context.Request.Query.Keys) { 
                    request.QueryStringParameters.Add(queryKvp, context.Request.Query[queryKvp]);
                }

                foreach (var headerKvp in context.Request.Headers) { 
                    request.Headers.Add(headerKvp.Key, headerKvp.Value.ToString());
                }
            }
            catch
            {

            }
            return request;
        }
    }
}
