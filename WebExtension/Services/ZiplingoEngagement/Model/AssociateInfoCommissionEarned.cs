using DirectScale.Disco.Extension;
using System.Collections.Generic;

namespace WebExtension.Services.ZiplingoEngagement.Model
{
    public class AssociateInfoCommissionEarned
    {
        public int AssociateId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public int SponsorId { get; set; }
        public int EnrollerId { get; set; }
        public string Birthdate { get; set; }
        public string SignupDate { get; set; }
        public int TotalWorkingYears { get; set; }
        public string LogoUrl { get; set; }
        public string CompanyName { get; set; }
        public string CompanyDomain { get; set; }
        public string EnrollerName { get; set; }
        public string EnrollerMobile { get; set; }
        public string EnrollerEmail { get; set; }
        public string SponsorName { get; set; }
        public string SponsorMobile { get; set; }
        public string SponsorEmail { get; set; }
        public bool CommissionActive { get; set; }
        public Dictionary<string, string> MerchantCustomFields { get; set; }
        public CommissionPaymentModel CommissionDetails { get; set; }
        public List<CommissionPaymentDetail> CommissionPaymentDetails { get; set; }
        public string CommissionNotes
        {
            get
            {
                string result = null;
                if ((CommissionDetails != null) & (CommissionPaymentDetails.Count > 0))
                {
                    result = ((CommissionPaymentDetails.Count != 1) ? $"Multiple Payables ({CommissionPaymentDetails.Count})" : CommissionPaymentDetails[0].Notes);
                }

                return result;
            }
        }
    }
}
