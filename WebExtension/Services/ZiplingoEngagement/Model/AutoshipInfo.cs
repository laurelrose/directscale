using System;

namespace WebExtension.Services.ZiplingoEngagement.Model
{
    public class AutoshipInfo
    {
        public int AutoshipId { get; set; }
        public int AssociateId { get; set; }
        public int UplineID { get; set; }
        public string BackOfficeID { get; set; }
        public DateTime NextProcessDate { get; set; }
        public DateTime StartDate { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string PrimaryPhone { get; set; }
        public string SponsorName { get; set; }
        public string SponsorEmail { get; set; }
        public string SponsorMobile { get; set; }
        public int OrderNumber { get; set; }
    }
}
