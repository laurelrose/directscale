using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebExtension.Helper.Models;

namespace WebExtension.Helper
{
    public static class CommonMethod
    {
        public static IEnumerable<List<T>> SplitList<T>(List<T> locations, int nSize = 30)
        {
            for (int i = 0; i < locations.Count; i += nSize)
            {
                yield return locations.GetRange(i, Math.Min(nSize, locations.Count - i));
            }
        }

        public static string GetCurrentMethodName(System.Reflection.MethodBase methodBase)
        {
            return methodBase.DeclaringType.Name.Split(new[] { '<', '>' })[1];
        }
        public static bool ValidateJSON(string data)
        {
            try
            {
                JToken.Parse(data);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static T Deserialize<T>(string data)
        {
            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(data);
            }
            catch
            {
                return default;
            }
        }
        public static string Serialize<T>(T data)
        {
            try
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(data);
            }
            catch
            {
                return null;
            }
        }

        public async static Task<(T, string)> ReadBodyFromContext<T>(HttpContext context)
        {
            T data = default;
            string body = string.Empty;
            using (StreamReader reader = new StreamReader(context.Request.Body, Encoding.UTF8))
            {
                body = await reader.ReadToEndAsync();
                if (CommonMethod.ValidateJSON(body))
                {
                    data = CommonMethod.Deserialize<T>(body);
                }
                else
                {

                }
            }
            return (data, body);
        }
    }
    public class Responses : Controller
    {
        public IActionResult OkResult(object obj = null)
        {
            APIResponse response = new APIResponse();
            response.Status = Ok().StatusCode.ToString();
            response.Data = obj;
            response.Message = APIResponseMessage.Success;
            return Ok(response);

        }

        public IActionResult BadRequestResult(object obj = null)
        {
            APIResponse response = new APIResponse();
            response.Status = BadRequest().StatusCode.ToString();
            response.Message = APIResponseMessage.Fail;
            response.Error = obj != null ? obj.ToString() : "";
            return BadRequest(response);

        }
        public IActionResult NotFoundResult(object obj = null)
        {
            APIResponse response = new APIResponse();
            response.Status = NotFound().StatusCode.ToString();
            response.Message = APIResponseMessage.Fail;
            response.Error = obj != null ? obj.ToString() : "";
            return NotFound(response);

        }
    }
}

