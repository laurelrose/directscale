using System;

namespace WebExtension.Models.GenericReports
{
    public class CustomReport
    {
        public int ReportId { get; set; }
        public string JsonObject { get; set; }
        public DateTime LastModified { get; set; }
        public string Name { get; set; }
        public bool Public { get; set; }
        public string User { get; set; }
        public ReportType ReportType { get; set; }
    }
}
