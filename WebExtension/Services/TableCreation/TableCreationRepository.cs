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
        [OrderCommissionDate] DATETIMEOFFSET NOT NULL,
        [OrderNumber] INT NOT NULL,
        [OrderAssociateId] INT NOT NULL,
        [OrderAssociateName] VARCHAR(255) NOT NULL,
        [OrderItemId] INT NOT NULL,
        [OrderItemSku] VARCHAR(255) NOT NULL,
        [OrderItemDescription] VARCHAR(255) NOT NULL,
        [OrderItemCredits] FLOAT NOT NULL,
        [CreditType] INT NOT NULL,
        [AwardedAssociateId] INT NOT NULL

        CONSTRAINT [RewardPointCredits_PrimaryKey] PRIMARY KEY CLUSTERED ([recordnumber] ASC)
    );

    -- Create necessary indexes on new table
    CREATE NONCLUSTERED INDEX [RewardPointCredits_OrderCommissionDate] ON [Client].[RewardPointCredits] ([OrderCommissionDate])
    INCLUDE (
        [recordnumber],
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
        [OrderNumber],
        [OrderCommissionDate],
        [OrderAssociateName],
        [OrderItemSku],
        [OrderItemDescription],
        [OrderItemCredits],
        [AwardedAssociateId]
    ) WITH (ONLINE = ON);

    -- Create new Client RewardPointCredits table type
    CREATE TYPE [Client].[RewardPointCredits] AS TABLE
    (
        [OrderCommissionDate] DATETIMEOFFSET NOT NULL,
        [OrderNumber] INT NOT NULL,
        [OrderAssociateId] INT NOT NULL,
        [OrderAssociateName] VARCHAR(255) NOT NULL,
        [OrderItemId] INT NOT NULL,
        [OrderItemSku] VARCHAR(255) NOT NULL,
        [OrderItemDescription] VARCHAR(255) NOT NULL,
        [OrderItemCredits] FLOAT NOT NULL,
        [CreditType] INT NOT NULL,
        [AwardedAssociateId] INT NOT NULL
    );
END;";
            await using (var dbConnection = new SqlConnection(await _dataService.GetClientConnectionString()))
            {
                await dbConnection.ExecuteAsync(createStatement);
            }
        }
    }
}
