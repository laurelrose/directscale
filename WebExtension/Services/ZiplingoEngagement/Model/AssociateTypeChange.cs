using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Services.ZiplingoEngagement.Model
{
    public class AssociateTypeChange
    {
        public string eventKey { get; set; }
        public int associateid { get; set; }
        public string companyname { get; set; }
        public string data { get; set; }
        public int associateStatus { get; set; }
        public int associateTypeId { get; set; }
    }
}
