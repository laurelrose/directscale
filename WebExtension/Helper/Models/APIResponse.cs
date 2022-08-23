using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Helper.Models
{
    public class APIResponse
    {
        public string Status { get; set; }
        public object Data { get; set; }
        public string Message { get; set; }
        public string Error { get; set; }
    }
    public static class APIResponseMessage
    {
        public static String Success { get { return "Success"; } }
        public static String Fail { get { return "Error"; } }
    }
}
