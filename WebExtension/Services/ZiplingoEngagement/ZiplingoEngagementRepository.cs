using System;
using WebExtension.Services.ZiplingoEngagementService.Model;
using System.Collections.Generic;
using Dapper;
using DirectScale.Disco.Extension.Services;
using System.Linq;
using WebExtension.Services.ZiplingoEngagement.Model;
using WebExtension.Models;

namespace WebExtension.Services.ZiplingoEngagementService
{
    public interface IZiplingoEngagementRepository
    {
        ZiplingoEngagementSettings GetSettings();
        void UpdateSettings(ZiplingoEngagementSettingsRequest settings);
        void ResetSettings();
        ShipInfo GetOrderNumber(int packageId);
        List<AssociateInfo> AssociateBirthdayWishesInfo();
        List<AssociateInfo> AssociateWorkAnniversaryInfo();
        AutoshipFromOrderInfo GetAutoshipFromOrder(int OrderNumber);
        string GetUsernameById(string associateId);
        string GetLastFoutDegitByOrderNumber(int orderId);
        string GetStatusById(int statusId);
        List<ZiplingoEventSettings> GetEventSettingsList();
        ZiplingoEventSettings GetEventSettingDetail(string eventKey);
        void UpdateEventSetting(ZiplingoEventSettingRequest request);
        //EWalletSettingModel GetEWalletSetting();
        List<ServiceInfo> GetServiceExpirationInfoBefore2Weeks();
    }
    public class ZiplingoEngagementRepository : IZiplingoEngagementRepository
    {
        private readonly IDataService _dataService;

        public ZiplingoEngagementRepository(
            IDataService dataService
            )
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        }

        

        public ZiplingoEngagementSettings GetSettings()
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
            {                
                var settingsQuery = "SELECT * FROM Client.ZiplingoEngagementSettings";

                return dbConnection.QueryFirstOrDefault<ZiplingoEngagementSettings>(settingsQuery);
            }
        }

        public List<ZiplingoEventSettings> GetEventSettingsList()
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                var parameter = new { };
                var query = @"SELECT * FROM Client.ZiplingoEventSettings ORDER BY recordnumber DESC";
                var result = dbConnection.Query<ZiplingoEventSettings>(query, parameter).ToList();
                return result;
            }
        }

        public ZiplingoEventSettings GetEventSettingDetail(string eventKey)
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                var parameter = new { eventKey };
                var query = @"SELECT * FROM Client.ZiplingoEventSettings WHERE eventKey = @eventKey";
                var result = dbConnection.QueryFirstOrDefault<ZiplingoEventSettings>(query, parameter);
                return result;
            }
        }
        public void UpdateEventSetting(ZiplingoEventSettingRequest request)
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                var parameters = new
                {
                    eventKey = request.eventKey,
                    Status = request.Status
                };

                var updateStatement = @"UPDATE Client.ZiplingoEventSettings SET Status = @Status WHERE eventKey = @eventKey";
                dbConnection.Execute(updateStatement, parameters);
            }
        }

        public string GetLastFoutDegitByOrderNumber(int orderId)
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                var parameter = new { orderId };
                var query = @"SELECT Number FROM ORD_PaymentGatewayTransactions WHERE OrderNumber = @orderId";

                var result = dbConnection.QueryFirstOrDefault<string>(query, parameter);
                return result;
            }
        }

        public void UpdateSettings(ZiplingoEngagementSettingsRequest settings)
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                var parameters = new
                {
                    settings.ApiUrl,
                    settings.Username,
                    settings.Password,
                    settings.LogoUrl,
                    settings.CompanyName
                };

                var updateStatement = @"UPDATE Client.ZiplingoEngagementSettings SET ApiUrl = @ApiUrl,  Username = @Username, Password = @Password, LogoUrl = @LogoUrl, CompanyName = @CompanyName";
                dbConnection.Execute(updateStatement, parameters);
            }
        }

        public void ResetSettings()
        {
            try
            {
                using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
                {
                    var settings = GetSettings();
                    var parameters = new
                    {
                        Username = "LaurelRoseAPI",
                        Password = "1264a6ec-fa9b-4c0a-9d2e-778b4fd4f82e",
                        ApiUrl = "http://unifiedbetaapi.ziplingo.com/api/",
                        LogoUrl = "https://laurelrose.corpadmin.directscale.com/CMS/Images/Logo-Default-230px.png",
                        settings.CompanyName
                    };

                    var query = @"MERGE INTO Client.[ZiplingoEngagementSettings] WITH (HOLDLOCK) AS TARGET 
                USING 
                    (SELECT @Username AS 'Username', @Password AS 'Password', @ApiUrl AS 'ApiUrl', @LogoUrl AS 'LogoUrl', @CompanyName AS 'CompanyName'
                ) AS SOURCE 
                    ON SOURCE.CompanyName = TARGET.CompanyName
                WHEN MATCHED THEN 
                    UPDATE SET TARGET.Username = SOURCE.Username, TARGET.Password = SOURCE.Password, TARGET.ApiUrl = SOURCE.ApiUrl, TARGET.LogoUrl = SOURCE.LogoUrl
                WHEN NOT MATCHED BY TARGET THEN 
                    INSERT (Username, [Password], ApiUrl, LogoUrl,CompanyName) 
					VALUES (SOURCE.Username, SOURCE.Password, SOURCE.ApiUrl, SOURCE.LogoUrl, SOURCE.CompanyName);";

                    dbConnection.Execute(query, parameters);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public ShipInfo GetOrderNumber(int packageId)
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                ShipInfo info = new ShipInfo();
                var query = $"SELECT o.recordnumber as OrderNumber,p.ShipMethodId,p.Carrier,p.DateShipped FROM ORD_OrderPackages p JOIN ORD_Order o ON p.OrderNumber = o.recordnumber WHERE p.recordnumber ='{packageId}'";
                using (var reader = dbConnection.ExecuteReader(query))
                {
                    if (reader.Read())
                    {
                        info.OrderNumber = (Int32)reader["OrderNumber"];
                        info.ShipMethodId = (Int32)reader["ShipMethodId"];
                        info.Carrier = Convert.ToString(reader["Carrier"]);
                        info.DateShipped = Convert.ToString(reader["DateShipped"]);
                    }
                }
                return info;
            }
        }
        public List<AssociateInfo> AssociateBirthdayWishesInfo()
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                List<AssociateInfo> objAsso = new List<AssociateInfo>();
                AssociateInfo info = new AssociateInfo();
                var query = $"SELECT Birthdate, recordnumber,EmailAddress,FirstName,LastName FROM crm_distributors WHERE datepart(dd, Birthdate) = datepart(dd, GETDATE()) AND datepart(mm, Birthdate) = datepart(mm, GETDATE()) AND Void = 0 AND StatusId<>5";

                using (var reader = dbConnection.ExecuteReader(query))
                {
                    while (reader.Read())
                    {
                        info.AssociateId = (Int32)reader["recordnumber"];
                        info.FirstName = Convert.ToString(reader["FirstName"]);
                        info.LastName = Convert.ToString(reader["LastName"]);
                        info.Birthdate = Convert.ToString(reader["Birthdate"]);
                        info.EmailAddress = Convert.ToString(reader["EmailAddress"]);
                        objAsso.Add(info);
                    }
                }
                return objAsso;
            }
        }

        public List<AssociateInfo> AssociateWorkAnniversaryInfo()
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                List<AssociateInfo> objAsso = new List<AssociateInfo>();
                AssociateInfo info = new AssociateInfo();
                var query = $"SELECT SignupDate, recordnumber,EmailAddress,FirstName,LastName,DATEDIFF (yy, SignupDate, GETDATE()) AS 'TotalWorkingYears' FROM crm_distributors WHERE datepart(dd, SignupDate) = datepart(dd, GETDATE()) AND datepart(mm, SignupDate) = datepart(mm, GETDATE()) AND Void = 0 AND StatusId<>5";
                using (var reader = dbConnection.ExecuteReader(query))
                {
                    while (reader.Read())
                    {
                        info.AssociateId = (Int32)reader["recordnumber"];
                        info.FirstName = Convert.ToString(reader["FirstName"]);
                        info.LastName = Convert.ToString(reader["LastName"]);
                        info.SignupDate = Convert.ToString(reader["SignupDate"]);
                        info.TotalWorkingYears = (Int32)reader["TotalWorkingYears"];
                        info.EmailAddress = Convert.ToString(reader["EmailAddress"]);
                        objAsso.Add(info);
                    }
                }
                return objAsso;
            }
        }

        public AutoshipFromOrderInfo GetAutoshipFromOrder(int orderNumber)
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                var qry = @"SELECT al.AutoShipId, a.LastProcessDate, a.NextProcessDate, a.Frequency
                FROM CRM_AutoShipLog al
                JOIN CRM_AutoShip a
                    ON al.AutoShipId = a.recordnumber
                WHERE al.OrderId = @orderNumber";

                return dbConnection.QueryFirstOrDefault<AutoshipFromOrderInfo>(qry, new { orderNumber });
            }
        }

        public string GetUsernameById(string associateId)
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                var parameters = new
                {
                    BackOfficeId = new Dapper.DbString { IsAnsi = true, Length = 25, Value = associateId }
                };
                var query = @"SELECT Username FROM Users WHERE BackOfficeId = @BackofficeId";

                return dbConnection.QueryFirstOrDefault<string>(query, parameters);
            }
        }
        public string GetStatusById(int statusId)
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                var parameter = new { statusId };
                var query = @"SELECT StatusName FROM CRM_AssociateStatuses WHERE recordnumber = @statusId";

                var result = dbConnection.QueryFirstOrDefault<string>(query, parameter);
                return result;
            }
        }

        //public EWalletSettingModel GetEWalletSetting()
        //{
        //    using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
        //    {
        //        var settingsQuery = "SELECT * FROM Client.Ewallet_Settings";

        //        return dbConnection.QueryFirstOrDefault<EWalletSettingModel>(settingsQuery);
        //    }
        //}

        public List<ServiceInfo> GetServiceExpirationInfoBefore2Weeks()
        {
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                var query = $@"
select distinct d.recordnumber as AssociateId,s.recordnumber as ServiceID, s.ServiceName,d.FirstName,d.LastName,acs.ExpirationDate, 
DATEDIFF(DAY, GetDate(), acs.ExpirationDate) as RemainingDays
 from [dbo].[CRM_AssociateServices] acs join [dbo].[CRM_Services] s on acs.ServiceId = s.recordnumber
join [dbo].[CRM_ServiceItems] si on s.recordnumber = si.ServiceID
join [dbo].[CRM_Distributors] d on acs.AssociateID = d.recordnumber
  where si.YearlyRenewal = 1 and acs.Void = 0 and d.StatusId=1 and 
  (DATEADD(dd, 0, DATEDIFF(dd, 0, acs.ExpirationDate)) = DATEADD(week,2,DATEADD(dd, 0, DATEDIFF(dd, 0, GETDATE()))) 
  OR DATEADD(dd, 0, DATEDIFF(dd, 0, acs.ExpirationDate)) = DATEADD(week,4,DATEADD(dd, 0, DATEDIFF(dd, 0, GETDATE()))) )
  order by expirationdate desc";

                var info = dbConnection.QueryFirstOrDefault<List<ServiceInfo>>(query);
                return info;
            }
        }
    }
}
