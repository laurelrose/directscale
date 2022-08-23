using System;

namespace WebExtension.Services.ZiplingoEngagementService.Model
{
    public class FivedayAutoshipModel
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
        public string LogoUrl { get; set; }
        public string CompanyName { get; set; }
        public string CompanyDomain { get; set; }
        public string SponsorName { get; set; }
        public string SponsorEmail { get; set; }
        public string SponsorMobile { get; set; }
        public int OrderNumber { get; set; }
        public string ErrorDetails { get; set; }
    }
}
