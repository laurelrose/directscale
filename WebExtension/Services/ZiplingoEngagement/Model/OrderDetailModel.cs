using DirectScale.Disco.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebExtension.Services.ZiplingoEngagement.Model
{
    public class OrderDetailModel
    {
        public string TrackingNumber { get; set; }
        public int ShipMethodId { get; set; }
        public string Carrier { get; set; }
        public string DateShipped { get; set; }
        public Order Order { get; set; }
        public int AutoshipId { get; set; }
    }
}
