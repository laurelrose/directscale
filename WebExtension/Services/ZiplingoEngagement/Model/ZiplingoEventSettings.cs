using System;

namespace WebExtension.Services.ZiplingoEngagementService.Model
{
    public class ZiplingoEventSettings
    {
        public int recordnumber { get; set; }
        public string eventKey { get; set; }
        public bool Status { get; set; }
        public DateTime last_Modified { get; set; }
        public DateTime created_Date { get; set; }
    }
}
