using Dapper;
using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using WebExtension.Services.ZiplingoEngagement.Model;

namespace WebExtension.Services.DailyRun
{
    public interface IDailyRunRepository
    {
        List<AutoshipInfo> GetNextFiveDayAutoships();
        List<CardInfo> GetCreditCardInfoBefore30Days();
    }
    public class DailyRunRepository : IDailyRunRepository
    {
        private readonly IDataService _dataService;
        public DailyRunRepository(IDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        }
        public List<AutoshipInfo> GetNextFiveDayAutoships()
        {
            using (var dbConnection = new SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                var parameters = new { DateFiveDayAfter = DateTime.Now.AddDays(5).Date };
                var sql = @"SELECT A.recordnumber as AutoshipId,A.AssociateId,A.NextProcessDate,A.StartDate,
                    D.FirstName,D.LastName,D.EmailAddress,D.PrimaryPhone,D.BackOfficeID,E.UplineID,
                    ED.FirstName +' '+ ED.LastName as SponsorName,ED.EmailAddress as SponsorEmail,ED.PrimaryPhone as SponsorMobile,
                    (SELECT TOP 1 OrderId FROM CRM_AutoShipLog WHERE CRM_AutoShipLog.AutoshipId=A.recordnumber Order BY last_modified desc) as OrderNumber
                    FROM CRM_AutoShip A
                    inner join crm_distributors D ON D.recordnumber=A.AssociateID
                    inner join CRM_EnrollTree E ON E.DistributorID=A.AssociateID 
                    inner join crm_distributors ED ON ED.recordnumber=E.UplineID WHERE NextProcessDate = @DateFiveDayAfter";
                var info = dbConnection.Query<AutoshipInfo>(sql, parameters).ToList();
                return info.ToList();
            }
        }
        public List<CardInfo> GetCreditCardInfoBefore30Days()
        {
            using (var dbConnection = new SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                var parameters = new { Before30DaysFromCurrentDate = DateTime.Now.AddDays(+30).Date };
                var sql = @"SELECT Input2 as Last4DegitOfCard,InputDate as ExpirationDate ,d.recordnumber as AssociateId,d.FirstName,d.LastName
					,d.PrimaryPhone,d.EmailAddress as Email
                    FROM crm_payments p Inner Join crm_distributors d on d.recordnumber=p.distributorID
                    WHERE p.Input2 IS NOT NULL AND p.InputDate IS NOT NULL AND CAST(InputDate as date) = CAST(@Before30DaysFromCurrentDate as date)";
                var info = dbConnection.Query<CardInfo>(sql, parameters).ToList();
                return info.ToList();
            }
        }
    }
}
