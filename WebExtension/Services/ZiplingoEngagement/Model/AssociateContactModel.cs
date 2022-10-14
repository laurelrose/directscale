using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebExtension.Services.ZiplingoEngagementService.Model
{
    public class AssociateContactModel
    {
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string UserName { get; set; }
        public string phoneNumber { get; set; }
        public string emailAddress { get; set; }
        public int SponsorId { get; set; }
        public int EnrollerId { get; set; }
        public int AssociateStatus { get; set; }
        public string address { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string zip { get; set; }
        public DateTime? birthday { get; set; }
        public string distributerId { get; set; }
        public string region { get; set; }
        public int AssociateId { get; set; }
        public string CompanyName { get; set; }
        public string WebAlias { get; set; }
        public string BackOfficeId { get; set; }
        public bool CommissionActive { get; set; }
        public string CountryCode { get; set; }
        public int AssociateType { get; set; }
        public string CompanyDomain { get; set; }
        public string CompanyUrl { get; set; }
        public string LanguageCode { get; set; }
        //public int HighestRank { get; set; }
        //public DateTime? lastAutoshipDate { get; set; }
        //public int paidAsRank { get; set; }
        public string EnrollerName { get; set; }
        public string EnrollerMobile { get; set; }
        public string EnrollerEmail { get; set; }
        public string SponsorName { get; set; }
        public string SponsorMobile { get; set; }
        public string SponsorEmail { get; set; }

        //OrderDate added
        public DateTime? OrderDate { get; set; }
        public DateTime JoinDate { get; set; }
        public bool ActiveAutoship { get; set; }
    }
}
