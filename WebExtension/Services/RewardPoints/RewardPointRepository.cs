using Dapper;
using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using WebExtension.Services.RewardPoints.Models;

namespace WebExtension.Services.RewardPoints
{
    public interface IRewardPointRepository
    {
        Task<CommissionPeriodInfo> GetCurrentCommissionPeriodInfoAsync();
        Dictionary<int, double> GetFirstTimeItemDiscounts(HashSet<int> itemIds);
        Dictionary<int, double> GetFirstTimeOrderCredits(HashSet<int> itemIds);
        HashSet<int> GetFirstTimeItemPurchases(int orderAssociateId, HashSet<int> itemIds);
        int GetFirstTimeOrderPurchaseCount(int orderAssociateId);
        Task<int> GetRepAssociateIdAsync(int orderAssociateId);
        Task SaveRewardPointCreditAsync(RewardPointCredit rewardPointCredit);
        Task SaveRewardPointCreditsAsync(List<RewardPointCredit> rewardPointCredits);
        Task<Dictionary<int, List<RewardPointCredit>>> GetRewardPointCreditsByAwardedAssociateIdAsync(DateTime endDate);
    }

    internal class RewardPointRepository : IRewardPointRepository
    {
        private readonly IDataService _dataService;

        public RewardPointRepository(IDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentException(nameof(dataService));
        }

        public async Task<CommissionPeriodInfo> GetCurrentCommissionPeriodInfoAsync()
        {
            const string sql = 
@"SELECT TOP 1 [recordnumber] AS CommissionPeriodId, [EndDate]
FROM [dbo].[CRM_CommissionPeriods]
WHERE [PeriodType] = 'Weekly' AND [CommitDate] IS NOT NULL
ORDER BY [recordnumber] DESC;";

            await using var dbConnection = new SqlConnection(await _dataService.GetClientConnectionString());
            return await dbConnection.QuerySingleAsync<CommissionPeriodInfo>(sql);
        }

        public Dictionary<int, double> GetFirstTimeItemDiscounts(HashSet<int> itemIds)
        {
            const string sql =
@"SELECT I.[recordnumber] AS ItemId, CAST(C.[Field2] AS FLOAT) AS FirstTimeItemDiscount
FROM [dbo].[INV_Inventory] I
JOIN [dbo].[INV_CustomFields] C ON C.[ItemID] = I.[recordnumber] AND ISNULL(C.[Field2], '') != ''
WHERE I.[recordnumber] IN (@ItemIds);";

            var itemDiscountsMap = new Dictionary<int, double>();
            using var dbConnection = new SqlConnection(_dataService.GetClientConnectionString().ConfigureAwait(false).GetAwaiter().GetResult());
            using var reader = dbConnection.ExecuteReader(sql, new { ItemIds = itemIds });
            while (reader.Read())
            {
                itemDiscountsMap.Add((int)reader["ItemId"], (double)reader["FirstTimeItemDiscount"]);
            }

            return itemDiscountsMap;
        }

        public Dictionary<int, double> GetFirstTimeOrderCredits(HashSet<int> itemIds)
        {
            const string sql =
                @"SELECT I.[recordnumber] AS ItemId, CAST(C.[Field1] AS FLOAT) AS FirstTimeOrderDiscount
FROM [dbo].[INV_Inventory] I
JOIN [dbo].[INV_CustomFields] C ON C.[ItemID] = I.[recordnumber] AND ISNULL(C.[Field1], '') != ''
WHERE I.[recordnumber] IN (@ItemIds);";

            var orderDiscountsMap = new Dictionary<int, double>();
            using var dbConnection = new SqlConnection(_dataService.GetClientConnectionString().ConfigureAwait(false).GetAwaiter().GetResult());
            using var reader = dbConnection.ExecuteReader(sql, new { ItemIds = itemIds });
            while (reader.Read())
            {
                orderDiscountsMap.Add((int)reader["ItemId"], (double)reader["FirstTimeOrderDiscount"]);
            }

            return orderDiscountsMap;
        }

        public HashSet<int> GetFirstTimeItemPurchases(int orderAssociateId, HashSet<int> itemIds)
        {
            const string sql =
@"SELECT [OrderItemId]
FROM [Client].[RewardPointCredits]
WHERE [OrderAssociateId] = @AssociateId
    AND [CreditType] = @CreditType
    AND [OrderItemId] IN (@ItemIds)
GROUP BY [OrderItemId]
HAVING COUNT([OrderItemId]) > 0;";

            var parameters = new
            {
                AssocaiteId = orderAssociateId,
                CreditType = (int)RewardPointCreditType.FirstTimeItemPurchase,
                ItemIds = itemIds
            };

            using var dbConnection = new SqlConnection(_dataService.GetClientConnectionString().ConfigureAwait(false).GetAwaiter().GetResult());
            var result = dbConnection.Query<int>(sql, parameters);
            return new HashSet<int>(result);
        }

        public int GetFirstTimeOrderPurchaseCount(int orderAssociateId)
        {
            const string sql =
@"SELECT COUNT(O.[recordnumber])
FROM [dbo].[ORD_Order] O
LEFT JOIN [dbo].[ORD_CustomFields] C ON C.[OrderNumber] = O.[recordnumber] AND C.[Field1] <> 'TRUE'
WHERE O.[DistributorID] = @AssociateId;";

            using var dbConnection = new SqlConnection(_dataService.GetClientConnectionString().ConfigureAwait(false).GetAwaiter().GetResult());
            return dbConnection.QueryFirstOrDefault<int>(sql, new { AssociateId = orderAssociateId });
        }

        public async Task<int> GetRepAssociateIdAsync(int orderAssociateId)
        {
            const string sql =
@"SELECT TOP 1 E.[AssociateID]
FROM RPT_GetEnrollmentUpline(@AssociateId) E
JOIN CRM_Distributors D ON E.[AssociateID] = D.[recordnumber]
WHERE D.[AssociateType] = 1;";

            await using var dbConnection = new SqlConnection(await _dataService.GetClientConnectionString());
            return await dbConnection.QueryFirstOrDefaultAsync<int>(sql, new { AssociateId = orderAssociateId });
        }

        public async Task SaveRewardPointCreditAsync(RewardPointCredit rewardPointCredit)
        {
            var insertStatement =
@"INSERT INTO [Client].[RewardPointCredits] ([OrderCommissionDate], [OrderNumber], [OrderAssociateId], [OrderAssociateName], [OrderItemId], [OrderItemSku], [OrderItemDescription], [OrderItemCredits], [CreditType], [AwardedAssociateId], [PayoutStatus])
VALUES (@OrderCommissionDate, @OrderNumber, @OrderAssociateId, @OrderAssociateName, @OrderItemId, @OrderItemSku, @OrderItemDescription, @OrderItemCredits, @CreditType, @AwardedAssociateId, @PayoutStatus);";

            await using var dbConnection = new SqlConnection(await _dataService.GetClientConnectionString());
            await dbConnection.ExecuteAsync(insertStatement, rewardPointCredit);
        }

        public async Task SaveRewardPointCreditsAsync(List<RewardPointCredit> rewardPointCredits)
        {
            var bulkInsertStatement =
@"INSERT INTO [Client].[RewardPointCredits] ([OrderCommissionDate], [OrderNumber], [OrderAssociateId], [OrderAssociateName], [OrderItemId], [OrderItemSku], [OrderItemDescription], [OrderItemCredits], [CreditType], [AwardedAssociateId], [PayoutStatus])
SELECT [OrderCommissionDate], [OrderNumber], [OrderAssociateId], [OrderAssociateName], [OrderItemId], [OrderItemSku], [OrderItemDescription], [OrderItemCredits], [CreditType], [AwardedAssociateId], [PayoutStatus]
FROM @RewardPointCredits TVP;";

            await using var dbConnection = new SqlConnection(await _dataService.GetClientConnectionString());
            await dbConnection.ExecuteAsync(bulkInsertStatement, new { RewardPointCredits = CreateSaveRewardPointCreditsTvp(rewardPointCredits) });
        }

        public async Task<Dictionary<int, List<RewardPointCredit>>> GetRewardPointCreditsByAwardedAssociateIdAsync(DateTime endDate)
        {
            var sql =
$@"SELECT [OrderCommissionDate]
    ,[OrderNumber]
    ,[OrderAssociateId]
    ,[OrderAssociateName]
    ,[OrderItemId]
    ,[OrderItemSku]
    ,[OrderItemDescription]
    ,[OrderItemCredits]
    ,[CreditType]
    ,[AwardedAssociateId]
    ,[PayoutStatus]
    ,[CommissionPeriodId]
FROM [Client].[RewardPointCredits]
WHERE [OrderCommissionDate] <= @EndDate
    AND [PayoutStatus] != {(int)PayoutStatus.Paid};";

            await using var dbConnection = new SqlConnection(await _dataService.GetClientConnectionString());
            var rewardPointCredits = await dbConnection.QueryAsync<RewardPointCredit>(sql, new { EndDate = endDate });
            var rewardPointCreditsByAwardedAssociateId = new Dictionary<int, List<RewardPointCredit>>();
            foreach (var rewardPointCredit in rewardPointCredits)
            {
                if (!rewardPointCreditsByAwardedAssociateId.ContainsKey(rewardPointCredit.AwardedAssociateId))
                {
                    rewardPointCreditsByAwardedAssociateId.Add(rewardPointCredit.AwardedAssociateId, new List<RewardPointCredit>());
                }

                rewardPointCreditsByAwardedAssociateId[rewardPointCredit.AwardedAssociateId].Add(rewardPointCredit);
            }

            return rewardPointCreditsByAwardedAssociateId;
        }

        private static SqlMapper.ICustomQueryParameter CreateSaveRewardPointCreditsTvp(List<RewardPointCredit> rewardPointCredits)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add(new DataColumn("OrderCommissionDate", typeof(DateTimeOffset)));
            dataTable.Columns.Add(new DataColumn("OrderNumber", typeof(int)));
            dataTable.Columns.Add(new DataColumn("OrderAssociateId", typeof(int)));
            dataTable.Columns.Add(new DataColumn("OrderAssociateName", typeof(string)));
            dataTable.Columns.Add(new DataColumn("OrderItemId", typeof(int)));
            dataTable.Columns.Add(new DataColumn("OrderItemSku", typeof(string)));
            dataTable.Columns.Add(new DataColumn("OrderItemDescription", typeof(string)));
            dataTable.Columns.Add(new DataColumn("OrderItemCredits", typeof(double)));
            dataTable.Columns.Add(new DataColumn("CreditType", typeof(int)));
            dataTable.Columns.Add(new DataColumn("AwardedAssociateId", typeof(int)));
            dataTable.Columns.Add(new DataColumn("PayoutStatus", typeof(int)));
            dataTable.Columns.Add(new DataColumn("CommissionPeriodId", typeof(int)) { AllowDBNull = true });

            if (rewardPointCredits != null && rewardPointCredits.Any())
            {
                foreach (var rewardPointCredit in rewardPointCredits)
                {
                    var row = dataTable.NewRow();
                    row["OrderCommissionDate"] = rewardPointCredit.OrderCommissionDate;
                    row["OrderNumber"] = rewardPointCredit.OrderNumber;
                    row["OrderAssociateId"] = rewardPointCredit.OrderAssociateId;
                    row["OrderAssociateName"] = rewardPointCredit.OrderAssociateName;
                    row["OrderItemId"] = rewardPointCredit.OrderItemId;
                    row["OrderItemSku"] = rewardPointCredit.OrderItemSku;
                    row["OrderItemDescription"] = rewardPointCredit.OrderItemDescription;
                    row["OrderItemCredits"] = rewardPointCredit.OrderItemCredits;
                    row["CreditType"] = (int)rewardPointCredit.CreditType;
                    row["AwardedAssociateId"] = rewardPointCredit.AwardedAssociateId;
                    row["PayoutStatus"] = (int)rewardPointCredit.PayoutStatus;
                    row["CommissionPeriodId"] = rewardPointCredit.CommissionPeriodId ?? (object)DBNull.Value;

                    dataTable.Rows.Add(row);
                }
            }

            return dataTable.AsTableValuedParameter("[Client].[RewardPointCredits]");
        }
    }
}
