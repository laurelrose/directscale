using System;

namespace WebExtension.Services.ZiplingoEngagement.Model
{
    public class AssociateStatusChange
    {
        public int OldStatusId { get; set; }
        public int NewStatusId { get; set; }
        public int AssociateId { get; set; }
        public string OldStatus { get; set; }
        public string NewStatus { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int SponsorId { get; set; }
        public int EnrollerId { get; set; }
        public string LogoUrl { get; set; }
        public string CompanyName { get; set; }
        public string CompanyDomain { get; set; }
        public string EnrollerName { get; set; }
        public string EnrollerMobile { get; set; }
        public string EnrollerEmail { get; set; }
        public string SponsorName { get; set; }
        public string SponsorMobile { get; set; }
        public string SponsorEmail { get; set; }
        public string WebAlias { get; set; }
        public string EmailAddress { get; set; }
    }
}
