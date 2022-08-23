using Dapper;
using DirectScale.Disco.Extension.Services;
using System;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace WebExtension.Repositories
{
    public interface ICustomLogRepository
    {
        Task SaveLog(int associateId, int orderId, string title, string message, string error, string url, string other, string request, string response);
    }
    public class CustomLogRepository : ICustomLogRepository
    {
        private readonly IDataService _dataService;

        public CustomLogRepository(IDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        }

        public async Task SaveLog(int associateId, int orderId, string title, string message, string error, string url, string other, string request, string response)
        {
            using (var dbConnection = new SqlConnection(await _dataService.GetClientConnectionString()))
            {
                var parameters = new
                {
                    associateId,
                    orderId,
                    title,
                    message = message.Replace("'", "''"),
                    error = error.Replace("'", "''"),
                    url,
                    other = other.Replace("'", "''"),
                    request = request.Replace("'", "''"),
                    response = response.Replace("'", "''")
                };
                var insertStatement = @"INSERT INTO [Client].[CustomLog](AssociateID,OrderID,Title,Message,Error,last_modified,Url,Request,Response,Other) VALUES(@associateId,@orderId,@title,@message,@error,GETDATE(),@url,@request,@response,@other)";
                await dbConnection.ExecuteAsync(insertStatement, parameters);
            }
        }
    }
}

/*CREATE TABLE [Client].[CustomLog](
	[recordnumber] [int] IDENTITY(1,1) primary key NOT NULL,
	[last_modified] [datetime] NOT NULL,
	[AssociateID] [int] NULL,
	[OrderID] [int] NULL,
	[Title] [varchar](max) NULL,
	[Message] [varchar](max) NULL,
	[Error] [varchar](max) NULL,
	[Url] [varchar](max) NULL,
	[Request] [varchar](max) NULL,
	[Response] [varchar](max) NULL,
	[Other] [varchar](max) NULL
)*/