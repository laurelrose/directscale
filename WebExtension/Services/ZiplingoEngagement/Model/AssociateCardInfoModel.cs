using System;

namespace WebExtension.Services.ZiplingoEngagementService.Model
{
    public class AssociateCardInfoModel
    {
        public int CardLast4Degit { get; set; }
        public DateTime CardDate { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string PrimaryPhone { get; set; }
        public string Email { get; set; }
        public string LogoUrl { get; set; }
        public string CompanyName { get; set; }
        public string CompanyDomain { get; set; }
        public string ErrorDetails { get; set; }
    }
}
