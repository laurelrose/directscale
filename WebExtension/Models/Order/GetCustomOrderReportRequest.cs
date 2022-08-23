using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Models
{
    public class GetCustomOrderReportRequest
    {
        public string begin { get; set; }
        public string end { get; set; }
        public string search { get; set; }
    }
}
