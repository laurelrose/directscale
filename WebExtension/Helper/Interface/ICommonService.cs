using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebExtension.Helper.Models;

namespace WebExtension.Helper.Interface
{
    public interface ICommonService
    {
        Task<RequestBasePara> GetCallRequestPara(HttpContext context);
    }
}
