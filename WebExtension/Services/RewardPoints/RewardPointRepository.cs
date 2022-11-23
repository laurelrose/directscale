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
        Task<HashSet<int>> GetFirstTimeItemPurchases(int associateId, IEnumerable<int> itemIds);
        Task<int> GetFirstTimeOrderPurchaseCount(int associateId);
        Task SaveRewardPointCredit(RewardPointCredit rewardPointCredit);
        Task SaveRewardPointCredits(List<RewardPointCredit> rewardPointCredits);
        Task<Dictionary<int, List<RewardPointCredit>>> GetRewardPointCreditsByAwardedAssociateId(DateTimeOffset beginDate, DateTimeOffset endDate);
    }

    internal class RewardPointRepository : IRewardPointRepository
    {
        private readonly IDataService _dataService;

        public RewardPointRepository(IDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentException(nameof(dataService));
        }

        public async Task<HashSet<int>> GetFirstTimeItemPurchases(int associateId, IEnumerable<int> itemIds)
        {
            const string sql =
@"SELECT [OrderItemId]
FROM [Client].[RewardPointCredits]
WHERE [OrderAssociateId] = @AssociateId
    AND [CreditType] = @CreditType
	AND [OrderItemId] IN (@OrderItemIds)
GROUP BY [OrderItemId]
HAVING COUNT([OrderItemId]) > 0;";

            var parameters = new
            {
                AssocaiteId = associateId,
                CreditType = (int)RewardPointCreditType.FirstTimeItemPurchase,
                OrderItemIds = itemIds
            };

            await using var dbConnection = new SqlConnection(await _dataService.GetClientConnectionString());
            var result = await dbConnection.QueryAsync<int>(sql, parameters);
            return new HashSet<int>(result);
        }

        public async Task<int> GetFirstTimeOrderPurchaseCount(int associateId)
        {
            const string sql =
@"SELECT COUNT(O.[recordnumber])
FROM [dbo].[ORD_Order] O
LEFT JOIN [dbo].[ORD_CustomFields] C ON C.[OrderNumber] = O.[recordnumber] AND C.[Field1] <> 'TRUE'
WHERE O.[DistributorID] = @AssociateId;";

            await using var dbConnection = new SqlConnection(await _dataService.GetClientConnectionString());
            return await dbConnection.QueryFirstOrDefaultAsync<int>(sql, new { AssociateId = associateId });
        }

        public async Task SaveRewardPointCredit(RewardPointCredit rewardPointCredit)
        {
            const string insertStatement =
@"INSERT INTO [Client].[RewardPointCredits] ([OrderCommissionDate], [OrderNumber], [OrderAssociateId], [OrderAssociateName], [OrderItemId], [OrderItemSku], [OrderItemDescription], [OrderItemCredits], [CreditType], [AwardedAssociateId])
VALUES (@OrderCommissionDate, @OrderNumber, @OrderAssociateId, @OrderAssociateName, @OrderItemId, @OrderItemSku, @OrderItemDescription, @OrderItemCredits, @CreditType, @AwardedAssociateId);";

            await using var dbConnection = new SqlConnection(await _dataService.GetClientConnectionString());
            await dbConnection.ExecuteAsync(insertStatement, rewardPointCredit);
        }

        public async Task SaveRewardPointCredits(List<RewardPointCredit> rewardPointCredits)
        {
            const string bulkInsertStatement =
@"INSERT INTO [Client].[RewardPointCredits] ([OrderCommissionDate], [OrderNumber], [OrderAssociateId], [OrderAssociateName], [OrderItemId], [OrderItemSku], [OrderItemDescription], [OrderItemCredits], [CreditType], [AwardedAssociateId])
SELECT [OrderCommissionDate], [OrderNumber], [OrderAssociateId], [OrderAssociateName], [OrderItemId], [OrderItemSku], [OrderItemDescription], [OrderItemCredits], [CreditType], [AwardedAssociateId]
FROM @RewardPointCredits TVP;";

            await using var dbConnection = new SqlConnection(await _dataService.GetClientConnectionString());
            await dbConnection.ExecuteAsync(bulkInsertStatement, new { RewardPointCredits = CreateSaveRewardPointCreditsTvp(rewardPointCredits) });
        }

        public async Task<Dictionary<int, List<RewardPointCredit>>> GetRewardPointCreditsByAwardedAssociateId(DateTimeOffset beginDate, DateTimeOffset endDate)
        {
            const string sql =
@"SELECT [OrderCommissionDate]
    ,[OrderNumber]
    ,[OrderAssociateId]
    ,[OrderAssociateName]
    ,[OrderItemId]
    ,[OrderItemSku]
    ,[OrderItemDescription]
    ,[OrderItemCredits]
    ,[CreditType]
    ,[AwardedAssociateId]
FROM [Client].[RewardPointCredits]
WHERE [OrderCommissionDate] > @BeginDate
    AND [OrderCommissionDate] <= @EndDate;";

            var parameters = new
            {
                BeginDate = beginDate,
                EndDate = endDate
            };

            await using var dbConnection = new SqlConnection(await _dataService.GetClientConnectionString());
            var rewardPointCredits = await dbConnection.QueryAsync<RewardPointCredit>(sql, parameters);
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

                    dataTable.Rows.Add(row);
                }
            }

            return dataTable.AsTableValuedParameter("[Client].[RewardPointCredits]");
        }
    }
}
