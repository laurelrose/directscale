using Dapper;
using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Repositories
{
    public interface IOrderWebRepository
    {
        List<int> GetFilteredOrderIds(string search, DateTime beginDate, DateTime endDate);
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
    }
}
