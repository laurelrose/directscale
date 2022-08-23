using System;
using WebExtension.Services.ZiplingoEngagement.Model;

namespace WebExtension.Services.ZiplingoEngagementService.Model
{
    public class ZiplingoEventSettingRequest : CommandRequest
    {
        public string eventKey { get; set; }
        public bool Status { get; set; }
    }
}
