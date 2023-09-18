using Dapper;
using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using WebExtension.Models.Order;

namespace WebExtension.Repositories
{
    public interface IOrderWebRepository
    {
        List<int> GetFilteredOrderIds(string search, DateTime beginDate, DateTime endDate);
        List<string> GetKitLevelFiveSkuList();
        int GetEnrollmentSponsorId(int associateId);
        List<CouponUsageDataModel> GetCouponUsageByOrderId(int orderNumber);
        int GetCouponUsageByAssociateID(int associateId,int couponID,DateTime startdate, DateTime enddate);
    }
    public class OrderWebRepository : IOrderWebRepository
    {
        private readonly IDataService _dataService;

        public OrderWebRepository(IDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        }
        public List<int> GetFilteredOrderIds(string search, DateTime beginDate, DateTime endDate)
        {
            using (var dbConnection = new SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                var parameters = new
                {
                    beginDate,
                    endDate
                };

                var queryStatement = $@"
                    SELECT DISTINCT
                            o.RecordNumber 
                    FROM ORD_Order o
                    JOIN CRM_Distributors D 
                        ON o.DistributorID = D.RecordNumber 
                    WHERE o.Void = 0
                        AND CAST(o.OrderDate AS DATE) >= @beginDate 
                        AND CAST(o.OrderDate AS DATE) <= @endDate
                    {BuildOrderFilterClause(search)}
                    ORDER BY o.RecordNumber DESC
                ";

                return dbConnection.Query<int>(queryStatement, parameters).ToList();
            }
        }
        private string BuildOrderFilterClause(string search)
        {
            var sql = string.Empty;

            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += string.Format(@" AND (
                        o.Name LIKE '%{0}%' OR 
                        o.Email LIKE '%{0}%' OR 
                        o.RecordNumber LIKE {0} OR 
                        o.SpecialInstructions LIKE '%{0}%' OR 
                        p.Reference LIKE '%{0}%' OR 
                        d.BackofficeID = '{0}')", search);
            }

            return sql;
        }

        public List<string> GetKitLevelFiveSkuList()
        {
            using (var dbConnection = new SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                var parameters = new{};

                var queryStatement = $@"
                    SELECT SKU FROM [dbo].[INV_Inventory] WHERE KitLevel=5
                ";

                return dbConnection.Query<string>(queryStatement, parameters).ToList();
            }
        }

        public int GetEnrollmentSponsorId(int associateId)
        {
            int sponsorId = 0;

            using (var dbConnection = new SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                var parameters = new
                {
                    AssociateId = associateId
                };

                var sql = @"SELECT 
	                        UplineID
                        FROM CRM_EnrollTree
                        WHERE 
	                        DistributorID = @AssociateId";
                sponsorId = dbConnection.QueryFirstOrDefault<int>(sql, parameters);
            }

            return sponsorId;
        }

        public List<CouponUsageDataModel> GetCouponUsageByOrderId(int orderNumber)
        {
            var parameters = new
            {
                OrderNumber = orderNumber
            };

            var sql = @"SELECT 
	                    cu.CouponID
	                    , Amount
                    FROM ORD_OrderTotals ot
                    INNER JOIN ORD_CouponUsage cu ON ot.recordnumber = cu.OrderTotalID
                    WHERE
	                    ot.OrderNumber = @OrderNumber";

            using (var connection = new SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                return connection.Query<CouponUsageDataModel>(sql, parameters).ToList();
            }
        }

        public int GetCouponUsageByAssociateID(int associateId,int couponID, DateTime startdate, DateTime enddate)
        {
            var parameters = new
            {
                AssociateID = associateId,CouponID = couponID, Startdate = startdate, Enddate = enddate
            };

            var sql = @"SELECT count(*) from ord_order o left join ord_ordertotals ot
                    on o.recordnumber = ot.ordernumber
                    left join ord_couponusage cu
                    on cu.OrderTotalID = ot.recordnumber
                    where o.distributorid = @AssociateID 
                    and 
                    couponid=@CouponID
                    and dateused between @StartDate and @EndDate";
            using (var connection = new SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                return connection.QueryFirstOrDefault<int>(sql, parameters);
            }
        }
    }
}
