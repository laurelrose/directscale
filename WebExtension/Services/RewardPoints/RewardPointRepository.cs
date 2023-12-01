using Dapper;
using DirectScale.Disco.Extension;
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
        Task<CommissionPeriodInfo> GetCurrentCommissionPeriodInfoAsync(int? comPeriodId);
        Dictionary<int, double> GetFirstTimeItemCredits(HashSet<int> itemIds);
        Dictionary<int, double> GetFirstTimeOrderCredits(HashSet<int> itemIds);
        int GetFirstTimeOrderPurchaseCount(int orderAssociateId);
        Task<int> GetRepAssociateIdAsync(NodeDetail[] nodeDetails, int orderAssociateId);
        Task<Dictionary<int, List<RewardPointCredit>>> GetAssociateRewardPointCredits(DateTime beginDate, DateTime endDate);
        Task<Dictionary<int, List<RewardPointCredit>>> GetAssociateRewardPointCreditsCustom();
        Task SaveRewardPointCreditAsync(RewardPointCredit rewardPointCredit);
        Task SaveRewardPointCreditsAsync(List<RewardPointCredit> rewardPointCredits);
        Task UpdateRewardPointCreditsAsync(List<RewardPointCredit> rewardPointCredits);
    }

    internal class RewardPointRepository : IRewardPointRepository
    {
        private readonly IDataService _dataService;

        public RewardPointRepository(IDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentException(nameof(dataService));
        }

        public async Task<CommissionPeriodInfo> GetCurrentCommissionPeriodInfoAsync(int? comPeriodId)
        {
            string whereStatement;
            if (comPeriodId.HasValue)
            {
                whereStatement = $"WHERE [recordnumber] = {comPeriodId.Value}";
            }
            else
            {
                whereStatement =
@"WHERE [PeriodType] = 'Weekly' AND [CommitDate] IS NOT NULL
ORDER BY [recordnumber] DESC";
            }

            var sql =
$@"SELECT TOP 1 [recordnumber] AS CommissionPeriodId, [BeginDate], [EndDate], [CommitDate]
FROM [dbo].[CRM_CommissionPeriods]
{whereStatement};";

            await using var dbConnection = new SqlConnection(await _dataService.GetClientConnectionString());
            var commissionPeriodInfo = await dbConnection.QuerySingleAsync<CommissionPeriodInfo>(sql);

            // Making sure to get the full begin and end date and time range
            commissionPeriodInfo.BeginDate = commissionPeriodInfo.BeginDate.Date;
            commissionPeriodInfo.EndDate = commissionPeriodInfo.EndDate.Date.AddDays(1).AddMilliseconds(-1);
            return commissionPeriodInfo;
        }

        public Dictionary<int, double> GetFirstTimeItemCredits(HashSet<int> itemIds)
        {
            const string sql =
@"SELECT I.[recordnumber] AS ItemId, CAST(C.[Field2] AS FLOAT) AS FirstTimeItemDiscount
FROM [dbo].[INV_Inventory] I
JOIN [dbo].[INV_CustomFields] C ON C.[ItemID] = I.[recordnumber] AND ISNULL(C.[Field2], '') != ''
WHERE I.[recordnumber] IN @ItemIds;";

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
WHERE I.[recordnumber] IN @ItemIds;";

            var orderDiscountsMap = new Dictionary<int, double>();
            using var dbConnection = new SqlConnection(_dataService.GetClientConnectionString().ConfigureAwait(false).GetAwaiter().GetResult());
            using var reader = dbConnection.ExecuteReader(sql, new { ItemIds = itemIds });
            while (reader.Read())
            {
                orderDiscountsMap.Add((int)reader["ItemId"], (double)reader["FirstTimeOrderDiscount"]);
            }

            return orderDiscountsMap;
        }

        public int GetFirstTimeOrderPurchaseCount(int orderAssociateId)
        {
            const string sql =
@"SELECT COUNT(O.[recordnumber])
FROM [dbo].[ORD_Order] O
WHERE O.[DistributorID] = @AssociateId
    AND O.[Void] = 0
    AND NOT EXISTS (
        SELECT 1
        FROM [dbo].[ORD_CustomFields] C
        WHERE C.[OrderNumber] = O.[recordnumber]
            AND C.[Field1] = 'TRUE'
    );";

            using var dbConnection = new SqlConnection(_dataService.GetClientConnectionString().ConfigureAwait(false).GetAwaiter().GetResult());
            return dbConnection.QueryFirstOrDefault<int>(sql, new { AssociateId = orderAssociateId });
        }

        public async Task<int> GetRepAssociateIdAsync(NodeDetail[] nodeDetails, int orderAssociateId)
        {
            const string sql =
@"SELECT TOP 1 D.[recordnumber]
FROM CRM_Distributors D
JOIN @UplineIds TVP ON TVP.[AssociateId] = D.[recordnumber]
WHERE D.[AssociateType] = 1
    AND D.[recordnumber] != @AssociateId
ORDER BY TVP.[Level] ASC;";

            var parameters = new
            {
                AssociateId = orderAssociateId,
                UplineIds = CreateNodeDetailsTvp(nodeDetails)
            };

            await using var dbConnection = new SqlConnection(await _dataService.GetClientConnectionString());
            return await dbConnection.QueryFirstOrDefaultAsync<int>(sql, parameters);
        }

        public async Task SaveRewardPointCreditAsync(RewardPointCredit rewardPointCredit)
        {
            var insertStatement =
@"INSERT INTO [Client].[RewardPointCredits] ([last_modified], [OrderCommissionDate], [OrderNumber], [OrderAssociateId], [OrderAssociateName], [OrderItemId], [OrderItemSku], [OrderItemQty], [OrderItemDescription], [OrderItemCredits], [CreditType], [AwardedAssociateId], [PayoutStatus])
VALUES (@LastModified, @OrderCommissionDate, @OrderNumber, @OrderAssociateId, @OrderAssociateName, @OrderItemId, @OrderItemSku, @OrderItemQty, @OrderItemDescription, @OrderItemCredits, @CreditType, @AwardedAssociateId, @PayoutStatus);";

            var parameters = new
            {
                LastModified = DateTime.Now,
                OrderCommissionDate = rewardPointCredit.OrderCommissionDate,
                OrderNumber = rewardPointCredit.OrderNumber,
                OrderAssociateId = rewardPointCredit.OrderAssociateId,
                OrderAssociateName = rewardPointCredit.OrderAssociateName,
                OrderItemId = rewardPointCredit.OrderItemId,
                OrderItemSku = rewardPointCredit.OrderItemSku,
                OrderItemQty = rewardPointCredit.OrderItemQty,
                OrderItemDescription = rewardPointCredit.OrderItemDescription,
                OrderItemCredits = rewardPointCredit.OrderItemCredits,
                CreditType = (int)rewardPointCredit.CreditType,
                AwardedAssociateId = rewardPointCredit.AwardedAssociateId,
                PayoutStatus = (int)rewardPointCredit.PayoutStatus
            };

            await using var dbConnection = new SqlConnection(await _dataService.GetClientConnectionString());
            await dbConnection.ExecuteAsync(insertStatement, parameters);
        }

        public async Task SaveRewardPointCreditsAsync(List<RewardPointCredit> rewardPointCredits)
        {
            var bulkInsertStatement =
@"INSERT INTO [Client].[RewardPointCredits] ([last_modified], [OrderCommissionDate], [OrderNumber], [OrderAssociateId], [OrderAssociateName], [OrderItemId], [OrderItemSku], [OrderItemQty], [OrderItemDescription], [OrderItemCredits], [CreditType], [AwardedAssociateId], [PayoutStatus])
SELECT [last_modified], [OrderCommissionDate], [OrderNumber], [OrderAssociateId], [OrderAssociateName], [OrderItemId], [OrderItemSku], [OrderItemQty], [OrderItemDescription], [OrderItemCredits], [CreditType], [AwardedAssociateId], [PayoutStatus]
FROM @RewardPointCredits TVP;";

            await using var dbConnection = new SqlConnection(await _dataService.GetClientConnectionString());
            await dbConnection.ExecuteAsync(bulkInsertStatement, new { RewardPointCredits = CreateSaveRewardPointCreditsTvp(rewardPointCredits) });
        }

        public async Task UpdateRewardPointCreditsAsync(List<RewardPointCredit> rewardPointCredits)
        {
            const string bulkUpdateStatement =
@"UPDATE R
SET R.[PayoutStatus] = TVP.[PayoutStatus], R.[CommissionPeriodId] = TVP.[CommissionPeriodId]
FROM [Client].[RewardPointCredits] R
JOIN @RewardPointCreditIds TVP ON TVP.[recordnumber] = R.[recordnumber];";

            await using var dbConnection = new SqlConnection(await _dataService.GetClientConnectionString());
            await dbConnection.ExecuteAsync(bulkUpdateStatement, new { RewardPointCreditIds = CreateUpdateRewardPointCreditsTvp(rewardPointCredits) });
        }

        public async Task<Dictionary<int, List<RewardPointCredit>>> GetAssociateRewardPointCredits(DateTime beginDate, DateTime endDate)
        {
            const string sql =
@"SELECT [recordnumber] AS Id
    ,[OrderCommissionDate]
    ,[OrderNumber]
    ,[OrderAssociateId]
    ,[OrderAssociateName]
    ,[OrderItemId]
    ,[OrderItemSku]
    ,[OrderItemQty]
    ,[OrderItemDescription]
    ,[OrderItemCredits]
    ,[CreditType]
    ,[AwardedAssociateId]
    ,[PayoutStatus]
    ,[CommissionPeriodId]
FROM [Client].[RewardPointCredits]
WHERE ([OrderCommissionDate] > @BeginDate AND [OrderCommissionDate] <= @EndDate AND [PayoutStatus] != @PaidPayoutStatus)
    OR ([OrderCommissionDate] <= @EndDate AND [PayoutStatus] = @ErrorPayoutStatus);";

            var parameters = new
            {
                BeginDate = beginDate,
                EndDate = endDate,
                ErrorPayoutStatus = (int)PayoutStatus.Error,
                PaidPayoutStatus = (int)PayoutStatus.Paid
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
        public async Task<Dictionary<int, List<RewardPointCredit>>> GetAssociateRewardPointCreditsCustom()
        {
            const string sql =
@"SELECT [recordnumber] AS Id
    ,[OrderCommissionDate]
    ,[OrderNumber]
    ,[OrderAssociateId]
    ,[OrderAssociateName]
    ,[OrderItemId]
    ,[OrderItemSku]
    ,[OrderItemQty]
    ,[OrderItemDescription]
    ,[OrderItemCredits]
    ,[CreditType]
    ,[AwardedAssociateId]
    ,[PayoutStatus]
    ,[CommissionPeriodId]
FROM [Client].[RewardPointCredits]
WHERE recordnumber in (
    967,968,969,970,971,972,973,974,975,976,977,978,979,980,981,982,983,984,985,986,987,988,989,990,991,992,993,994,995,997,998,999,1000,1001,1002,1003,1005,1006,1007,1009,1010,1011,1012,1013,1015,1016,1017,1328,1021,1022,1023,1024,1025,1026,1027,1028,1029,1030,1031,1032,1033,1034,1035,1038,1039,1040,1041,1042,1043,1044,1046,1047,1048,1049,1050,1051,1052,1053,1054,1055,1056,1057,1058,1059,1060,1061,1062,1063,1064,1065,1066,1067,1068,1069,1070,1071,1072,1073,1074,1075,1076,1077,1078,1079,1080,1081,1082,1083,1084,1085,1086,1087,1088,1089,1090,1091,1092,1093,1094,1096,1097,1098,1100,1102,1103,1104,1105,1107,1108,1109,1110,1111,1112,1113,1114,1115,1116,1117,1118,1119,1120,1121,1122,1123,1124,1125,1126,1127,1129,1130,1131,1132,1133,1134,1135,1136,1138,1139,1140,1142,1143,1144,1145,1146,1147,1148,1150,1152,1153,1154,1155,1156,1157,1158,1159,1160,1161,1163,1164,1166,1167,1168,1169,1170,1172,1173,1174,1175,1176,1177,1178,1179,1180,1181,1182,1183,1184,1185,1186,1187,1188,1189,1190,1191,1192,1193,1194,1195,1196,1197,1198,1199,1200,1201,1202,1203,1204,1205,1206,1207,1208,1209,1210,1211,1212,1213,1214,1216,1218,1220,1221,1223,1225,1227,1228,1229,1230,1232,1233,1234,1235,1236,1237,1238,1239,1241,1242,1243,1244,1245,1246,1247,1249,1250,1251,1252,1254,1256,1258,1260,1261,1262,1263,1264,1329,1266,1268,1269,1270,1271,1272,1273,1274,1276,1330,1278,1279,1280,1281,1282,1284,1285,1287,1288,1290,1292,1293,1294,1295,1297,1298,1301,1302,1304,1305,1306,1307,1310,1311,1331,1332,1312,1333,1313,1315,1317,1318,1319,1321,1334,1335,1336,1337,1338,1339,1340,1341,1342,1343,1344,1345,1346,1347,1348,1349,1350,1351,1352,1353,1354,1355,1356,1357,1358,1359,1360,1361,1362,1363,1322,1364,1365,1366,1323,1324,1325,1326,1367,1368,1369,1370,1371
) and PayoutStatus <> 10";

           

            await using var dbConnection = new SqlConnection(await _dataService.GetClientConnectionString());
            var rewardPointCredits = await dbConnection.QueryAsync<RewardPointCredit>(sql);
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
            dataTable.Columns.Add(new DataColumn("last_modified", typeof(DateTime)));
            dataTable.Columns.Add(new DataColumn("OrderCommissionDate", typeof(DateTime)));
            dataTable.Columns.Add(new DataColumn("OrderNumber", typeof(int)));
            dataTable.Columns.Add(new DataColumn("OrderAssociateId", typeof(int)));
            dataTable.Columns.Add(new DataColumn("OrderAssociateName", typeof(string)));
            dataTable.Columns.Add(new DataColumn("OrderItemId", typeof(int)));
            dataTable.Columns.Add(new DataColumn("OrderItemSku", typeof(string)));
            dataTable.Columns.Add(new DataColumn("OrderItemQty", typeof(double)));
            dataTable.Columns.Add(new DataColumn("OrderItemDescription", typeof(string)));
            dataTable.Columns.Add(new DataColumn("OrderItemCredits", typeof(double)));
            dataTable.Columns.Add(new DataColumn("CreditType", typeof(int)));
            dataTable.Columns.Add(new DataColumn("AwardedAssociateId", typeof(int)));
            dataTable.Columns.Add(new DataColumn("PayoutStatus", typeof(int)));
            dataTable.Columns.Add(new DataColumn("CommissionPeriodId", typeof(int)) { AllowDBNull = true });

            if (rewardPointCredits != null && rewardPointCredits.Any())
            {
                var now = DateTime.Now;
                foreach (var rewardPointCredit in rewardPointCredits)
                {
                    var row = dataTable.NewRow();
                    row["last_modified"] = now;
                    row["OrderCommissionDate"] = rewardPointCredit.OrderCommissionDate;
                    row["OrderNumber"] = rewardPointCredit.OrderNumber;
                    row["OrderAssociateId"] = rewardPointCredit.OrderAssociateId;
                    row["OrderAssociateName"] = rewardPointCredit.OrderAssociateName;
                    row["OrderItemId"] = rewardPointCredit.OrderItemId;
                    row["OrderItemSku"] = rewardPointCredit.OrderItemSku;
                    row["OrderItemQty"] = rewardPointCredit.OrderItemQty;
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

        private static SqlMapper.ICustomQueryParameter CreateUpdateRewardPointCreditsTvp(List<RewardPointCredit> rewardPointCredits)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add(new DataColumn("recordnumber", typeof(int)));
            dataTable.Columns.Add(new DataColumn("last_modified", typeof(DateTime)));
            dataTable.Columns.Add(new DataColumn("PayoutStatus", typeof(int)));
            dataTable.Columns.Add(new DataColumn("CommissionPeriodId", typeof(int)) { AllowDBNull = true });

            if (rewardPointCredits != null && rewardPointCredits.Any())
            {
                var now = DateTime.Now;
                foreach (var rewardPointCredit in rewardPointCredits)
                {
                    var row = dataTable.NewRow();
                    row["recordnumber"] = rewardPointCredit.Id;
                    row["last_modified"] = now;
                    row["PayoutStatus"] = (int)rewardPointCredit.PayoutStatus;
                    row["CommissionPeriodId"] = rewardPointCredit.CommissionPeriodId ?? (object)DBNull.Value;

                    dataTable.Rows.Add(row);
                }
            }

            return dataTable.AsTableValuedParameter("[Client].[RewardPointCredits_Update]");
        }

        private static SqlMapper.ICustomQueryParameter CreateNodeDetailsTvp(NodeDetail[] nodeDetails)
        {
            var dataTable = new DataTable();
            dataTable.Columns.Add(new DataColumn("AssociateId", typeof(int)));
            dataTable.Columns.Add(new DataColumn("Level", typeof(int)));

            if (nodeDetails != null && nodeDetails.Any())
            {
                foreach (var nodeDetail in nodeDetails)
                {
                    var row = dataTable.NewRow();
                    row["AssociateId"] = nodeDetail.NodeId.AssociateId;
                    row["Level"] = nodeDetail.Level;

                    dataTable.Rows.Add(row);
                }
            }

            return dataTable.AsTableValuedParameter("[Client].[AssociateUplineInfo]");
        }
    }
}
