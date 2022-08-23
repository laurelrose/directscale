using DirectScale.Disco.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebExtension.Services.ZiplingoEngagement.Model
{
    public class AutoshipFromOrderInfo
    {
        public int AutoshipId { get; set; }
        public DateTime LastProcessDate { get; set; }
        public DateTime NextProcessDate { get; set; }
        public Frequency Frequency { get; set; }
        public bool IsManual { get; set; }
    }
}
