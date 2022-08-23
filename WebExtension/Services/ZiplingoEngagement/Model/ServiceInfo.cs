using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Services.ZiplingoEngagement.Model
{
    public class ServiceInfo
    {
        public int AssociateId { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string RemainingDays { get; set; }
    }
}
