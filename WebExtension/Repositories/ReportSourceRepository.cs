using Dapper;
using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using WebExtension.Models.GenericReports;

namespace WebExtension.Repositories
{
    public interface IReportSourceRepository
    {
        List<ReportKey> GetReportKeys();
        Source[] GetSources();
        Dictionary<int, List<string>> LoadColumns();
        Dictionary<int, List<SourceInput>> LoadInputs();
        Dictionary<int, List<SourceKey>> LoadSourceKeys();
    }
    public class ReportSourceRepository : IReportSourceRepository
    {
        private readonly IDataService _dataService;

        public ReportSourceRepository(IDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        }

        public List<ReportKey> GetReportKeys()
        {
            var reportKeys = new List<ReportKey>();
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                var reader = dbConnection.ExecuteReader("SELECT [recordnumber], [Name], [Color] FROM [dbo].[ReportKeys]");
                while (reader.Read())
                {
                    reportKeys.Add(new ReportKey
                    {
                        Key = (int)reader["recordnumber"],
                        Color = reader["Color"] as string ?? string.Empty,
                        Name = reader["Name"] as string ?? string.Empty
                    });
                }
            }

            return reportKeys;
        }

        public Source[] GetSources()
        {
            var res = new List<Source>();
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                var reader = dbConnection.ExecuteReader("SELECT [recordnumber], [Name], [Query] FROM [dbo].[ReportSources]");
                while (reader.Read())
                {
                    res.Add(new Source
                    {
                        Id = (int)reader["recordnumber"],
                        Name = reader["Name"] as string ?? string.Empty,
                        Query = reader["Query"] as string ?? string.Empty
                    });
                }
            }

            return res.ToArray();
        }

        public Dictionary<int, List<string>> LoadColumns()
        {
            var columns = new Dictionary<int, List<string>>();
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                var reader = dbConnection.ExecuteReader("SELECT [SourceID], [Field] FROM [dbo].[ReportFields]");
                while (reader.Read())
                {
                    var sourceId = (int)reader["SourceID"];
                    if (!columns.ContainsKey(sourceId))
                    {
                        columns.Add(sourceId, new List<string>());
                    }

                    columns[sourceId].Add(reader["Field"] as string ?? string.Empty);
                }
            }

            return columns;
        }

        public Dictionary<int, List<SourceInput>> LoadInputs()
        {
            var res = new Dictionary<int, List<SourceInput>>();
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                var rdr = dbConnection.ExecuteReader("SELECT [SourceID], [Name], [Type], [InputSource], [QueryKey], [Value] FROM [dbo].[ReportSourcesInput]");
                while (rdr.Read())
                {
                    var sourceId = (int)rdr["SourceID"];
                    if (!res.ContainsKey(sourceId))
                    {
                        res.Add(sourceId, new List<SourceInput>());
                    }

                    res[sourceId].Add(new SourceInput
                    {
                        Name = rdr["Name"] as string ?? string.Empty,
                        Type = rdr["Type"] as int? ?? 0,
                        QueryKey = rdr["QueryKey"] as string ?? string.Empty,
                        Value = rdr["Value"] as string ?? string.Empty,
                        InputSource = rdr["InputSource"] as string ?? string.Empty,
                        SourceId = sourceId
                    });
                }
            }

            return res;
        }

        public Dictionary<int, List<SourceKey>> LoadSourceKeys()
        {
            var res = new Dictionary<int, List<SourceKey>>();
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
            {
                var reader = dbConnection.ExecuteReader("SELECT [SourceID], [Key], [JoinField], [SubKey] FROM [dbo].[ReportSourcesKeys]");
                while (reader.Read())
                {
                    var sourceId = (int)reader["SourceID"];
                    if (!res.ContainsKey(sourceId))
                    {
                        res.Add(sourceId, new List<SourceKey>());
                    }

                    res[sourceId].Add(new SourceKey
                    {
                        Key = reader["Key"] as int? ?? 0,
                        JoinField = reader["JoinField"] as string ?? string.Empty,
                        SubKey = reader["SubKey"] as int? ?? 0,
                        SourceId = sourceId
                    });
                }
            }

            return res;
        }

    }
}
