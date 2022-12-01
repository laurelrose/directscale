using Dapper;
using DirectScale.Disco.Extension.Services;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace WebExtension.Services.TableCreation
{
    public interface ITableCreationRepository
    {
        public Task CreateRewardPointCreditTable();
    }

    internal class TableCreationRepository : ITableCreationRepository
    {
        private readonly IDataService _dataService;

        public TableCreationRepository(IDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        }

        public async Task CreateRewardPointCreditTable()
        {
            const string createStatement =
@"IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = N'RewardPointCredits' AND [type] = 'U' AND [schema_id] = (SELECT [schema_id] FROM sys.schemas WHERE [name] = 'Client'))
BEGIN
    -- Create Client RewardPointCredits table
	CREATE TABLE [Client].[RewardPointCredits]
	(
		[recordnumber] INT IDENTITY,
		[last_modified] DATETIME NOT NULL DEFAULT GETDATE(),
		[OrderCommissionDate] DATETIME NOT NULL,
		[OrderNumber] INT NOT NULL,
		[OrderAssociateId] INT NOT NULL,
		[OrderAssociateName] VARCHAR(255) NOT NULL,
		[OrderItemId] INT NOT NULL,
		[OrderItemSku] VARCHAR(255) NOT NULL,
		[OrderItemDescription] VARCHAR(255) NOT NULL,
		[OrderItemCredits] FLOAT NOT NULL,
		[CreditType] INT NOT NULL,
		[AwardedAssociateId] INT NOT NULL,
		[PayoutStatus] INT NOT NULL,
		[CommissionPeriodId] INT

		CONSTRAINT [RewardPointCredits_PrimaryKey] PRIMARY KEY CLUSTERED ([recordnumber] ASC)
	);

	-- Create necessary indexes on new table
	CREATE NONCLUSTERED INDEX [RewardPointCredits_OrderCommissionDate_PayoutStatus_CommissionPeriodId] ON [Client].[RewardPointCredits] ([OrderCommissionDate], [PayoutStatus], [CommissionPeriodId])
	INCLUDE (
		[recordnumber],
		[last_modified],
		[OrderNumber],
		[OrderAssociateId],
		[OrderAssociateName],
		[OrderItemId],
		[OrderItemSku],
		[OrderItemDescription],
		[OrderItemCredits],
		[CreditType],
		[AwardedAssociateId]
	) WITH (ONLINE = ON);

	CREATE NONCLUSTERED INDEX [RewardPointCredits_OrderAssociateId_CreditType_OrderItemId] ON [Client].[RewardPointCredits] ([OrderAssociateId], [CreditType], [OrderItemId])
	INCLUDE (
		[recordnumber],
		[last_modified],
		[OrderNumber],
		[OrderCommissionDate],
		[OrderAssociateName],
		[OrderItemSku],
		[OrderItemDescription],
		[OrderItemCredits],
		[AwardedAssociateId],
		[PayoutStatus],
		[CommissionPeriodId]
	) WITH (ONLINE = ON);

	-- Create new Client RewardPointCredits table types
	CREATE TYPE [Client].[RewardPointCredits] AS TABLE
	(
	    [last_modified] DATETIME NOT NULL,
		[OrderCommissionDate] DATETIME NOT NULL,
		[OrderNumber] INT NOT NULL,
		[OrderAssociateId] INT NOT NULL,
		[OrderAssociateName] VARCHAR(255) NOT NULL,
		[OrderItemId] INT NOT NULL,
		[OrderItemSku] VARCHAR(255) NOT NULL,
		[OrderItemDescription] VARCHAR(255) NOT NULL,
		[OrderItemCredits] FLOAT NOT NULL,
		[CreditType] INT NOT NULL,
		[AwardedAssociateId] INT NOT NULL,
		[PayoutStatus] INT NOT NULL,
		[CommissionPeriodId] INT
	);

	CREATE TYPE [Client].[RewardPointCredits_Update] AS TABLE
	(
		[recordnumber] INT,
		[last_modified] DATETIME NOT NULL,
		[PayoutStatus] INT NOT NULL,
		[CommissionPeriodId] INT
	);
END;";
            await using (var dbConnection = new SqlConnection(await _dataService.GetClientConnectionString()))
            {
                await dbConnection.ExecuteAsync(createStatement);
            }
        }
    }
}
