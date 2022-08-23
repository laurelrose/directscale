using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebExtension.Services.ZiplingoEngagementService.Model
{
    public class ZiplingoEngagementRequest
    {
        public string eventKey { get; set; }
        public int associateid { get; set; }
        public string companyname { get; set; } 
        public string data { get; set; }
        public int associateStatus { get; set; }
        public int rankid { get; set; }
    }
}
