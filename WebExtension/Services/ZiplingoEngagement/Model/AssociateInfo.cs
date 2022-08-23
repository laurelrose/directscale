using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebExtension.Services.ZiplingoEngagement.Model
{
    public class AssociateInfo
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
    }
}
