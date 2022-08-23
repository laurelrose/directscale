using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Helper.Models
{
    public class RequestBasePara
    {
        public Dictionary<string, string> QueryStringParameters { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }
}
