using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using WebExtension.Models.GenericReports;
using Dapper;
using Newtonsoft.Json;
using WebExtension.Reports;

namespace WebExtension.Repositories
{

    public interface IGenericReportRepository
    {
        QueryResult GetReportDetails(int recordnumber, int maxRowCount, string replaceChars = "");
        //Task<List<string>> GetReportDetails1(int recordnumber);
    }
    public class GenericReportRepository : IGenericReportRepository
    {
        private const int CommandTimeoutSeconds = 240;
        private readonly IDataService _dataService;
        private readonly IReportSourceManager _reportSourceManager;
        public GenericReportRepository(IDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        }


        public CustomReport GetCustomReportById(int reportId)
        {
            const string sql =
            @"SELECT [recordnumber]
                ,[last_modified]
                ,[Name]
                ,[User]
                ,[Public]
                ,[JsonObject]
                ,[ReportType]
            FROM [dbo].[Reports]
            WHERE [recordnumber] = @ReportId;";

            CustomReport customReport = null;
            using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
            {

                var reader = dbConnection.ExecuteReader(sql, new { ReportId = reportId });
                if (reader.Read())
                {
                    customReport = new CustomReport
                    {
                        ReportId = (int)reader["recordnumber"],
                        JsonObject = reader["JsonObject"] as string,
                        LastModified = (DateTime)reader["last_modified"],
                        Public = reader["Public"] as bool? ?? true,
                        Name = reader["Name"] as string,
                        ReportType = (ReportType)(int)reader["ReportType"],
                        User = reader["User"] as string
                    };
                }
            }

            return customReport;
        }

        private string GetReportQuery(int reportId)
        {
            string queryDetails = "";

            var customReport = GetCustomReportById(reportId);

            if (customReport == null)
            {
                throw new Exception($"Could not find Custom Report with ID '{reportId}'");
            }

            SaveItemInfo saveItemInfo;
            try
            {
                saveItemInfo = JsonConvert.DeserializeObject<SaveItemInfo>(customReport.JsonObject);
                if (saveItemInfo.Items == null) saveItemInfo.Items = new MyItem[] { };
                if (saveItemInfo.ColInfo == null) saveItemInfo.ColInfo = new ColumnIndexInfo[] { };
                if (saveItemInfo.Sort == null) saveItemInfo.Sort = new SortOrderInfo[] { };

                queryDetails = BuildSQuery(saveItemInfo.Items, saveItemInfo.Sort, null, null);
            }
            catch (Exception e)
            {
                throw;
            }
            return queryDetails;
        }


        private bool ShowColumns(string sourceId, MyItem[] items)
        {
            foreach (MyItem item in items)
            {
                if (item.ID == sourceId) return item.ShowColumns;
            }

            return true;
        }

        private int GetLinkScore(MyItem[] items, List<string> stack, bool inverce)
        {
            return GetLinkScore_Rec(items, stack, stack.Count - 1, inverce);
        }

        private int GetLinkScore_Rec(MyItem[] items, List<string> stack, int index, bool inverce)
        {
            MyItem a = null;
            MyItem b = null;

            int score = 0;

            if (index > 0)
            {
                foreach (MyItem item in items)
                {
                    if (item.ID == stack[index])
                    {
                        b = item;
                    }

                    if (item.ID == stack[index - 1])
                    {
                        a = item;
                    }
                }
            }

            if (a != null && b != null)
            {
                foreach (string link in a.Links)
                {
                    foreach (string sLink in b.Links)
                    {
                        if (sLink == link)
                        {
                            score++;
                        }
                    }
                }
            }

            if (index > 1)
            {
                int prop = GetLinkScore_Rec(items, stack, index - 1, inverce);

                if (inverce)
                {
                    if (prop > score) return prop;
                }
                else
                {
                    if (prop < score) return prop;
                }
            }

            return score;
        }

        private bool AllLinked(MyItem[] items, Dictionary<string, List<string>> links)
        {
            foreach (MyItem item in items)
            {
                bool isStacked = false;

                foreach (string key in links.Keys)
                {
                    if (links[key][links[key].Count - 1] == item.ID) isStacked = true;
                }

                if (!isStacked) return false;
            }

            return true;
        }

        private int GetLinkedScore(MyItem[] items, Dictionary<string, List<string>> links)
        {
            if (!AllLinked(items, links)) return 0;

            int score = 0;

            foreach (string key in links.Keys)
            {
                score += GetLinkScore(items, links[key], false);
            }

            return score;
        }
        private bool BuildLnkStack(MyItem[] items, MyItem from, MyItem to, List<string> oldStack, out List<string> stack)
        {
            stack = new List<string>();
            stack.Add(to.ID);

            bool res = false;
            List<List<string>> potStack = new List<List<string>>();

            if (from == to) return true;

            foreach (string link in to.Links)
            {
                foreach (string sLink in from.Links)
                {
                    if (sLink == link)
                    {
                        List<string> tList = new List<string>(stack.ToArray());
                        tList.Add(from.ID);
                        potStack.Add(tList);
                        res = true;
                    }
                }
            }

            foreach (MyItem item in items)
            {
                if (item != to && item != from && !oldStack.Contains(item.ID))
                {
                    foreach (string link in to.Links)
                    {
                        foreach (string sLink in item.Links)
                        {
                            if (sLink == link)
                            {
                                List<string> pStack = new List<string>(oldStack.ToArray());
                                pStack.Add(to.ID);
                                List<string> cStack;
                                if (BuildLnkStack(items, from, item, pStack, out cStack))
                                {
                                    //Check for High Score Here!!
                                    //stack.AddRange(cStack.ToArray());
                                    //return true;
                                    List<string> tList = new List<string>(stack.ToArray());
                                    tList.AddRange(cStack.ToArray());
                                    potStack.Add(tList);
                                    res = true;
                                }
                            }
                        }
                    }
                }
            }

            if (res)
            {
                int bestIndex = 0;
                int bestScore = 0;

                for (int i = 0; i < potStack.Count; i++)
                {
                    int tScore = GetLinkScore(items, potStack[i], true);
                    if (tScore > bestScore)
                    {
                        bestScore = tScore;
                        bestIndex = i;
                    }
                }

                stack = potStack[bestIndex];
                return true;
            }

            return false;
        }

        private string ReplaceFields(string qry, string sourceId, MyItem[] items)
        {
            foreach (MyItem item in items)
            {
                if (item.ID == sourceId)
                {
                    foreach (ItemInput input in item.Inputs)
                    {
                        if (input.Type == 4)
                        {
                            string[] parts = input.Value.Split(',');
                            string val = string.Empty;
                            bool nfst = false;
                            foreach (string part in parts)
                            {
                                if (nfst)
                                {
                                    val += ", ";
                                }

                                val += $"'{part.Trim()}'";
                                nfst = true;
                            }

                            qry = qry.Replace("{{" + input.ID + "}}", val);
                        }
                        else
                        {
                            qry = qry.Replace("{{" + input.ID + "}}", input.Value);
                        }
                    }
                }
            }

            qry = ReplaceBox(qry, sourceId, items);

            return qry;
        }

        private string ReplaceBox(string qryString, string sourceId, MyItem[] items)
        {
            string subIn = "";
            MyItem subItem = null;
            foreach (MyItem item in items)
            {
                if (item.ID == sourceId) subItem = item;
            }

            if (subItem != null)
            {
                if (subItem.Boxes.Length > 0)
                {
                    foreach (var sourceKey in _reportSourceManager.GetSourceKeys(Convert.ToInt32(subItem.ID), 1))
                    {
                        subIn = sourceKey.JoinField + " in (";
                        subIn += BuildSQuery(subItem.Boxes, null, subItem.Boxes[0].ID, sourceKey.Key.ToString());
                        subIn += ") X";
                    }
                }
            }

            qryString = qryString.Replace("{{SUBKEY}}", subIn);

            return qryString;
        }

        private string BuildQuery(string sourceId, Dictionary<string, List<string>> stacks, MyItem[] items, List<string> built, Dictionary<string, string> avKeys, string skip)
        {
            Dictionary<string, MyItem> myItems = new Dictionary<string, MyItem>();
            foreach (MyItem item in items)
            {
                myItems.Add(item.ID, item);
            }

            string queryString = "";
            Dictionary<string, string> joinKeys = new Dictionary<string, string>();

            foreach (var sourceKey in _reportSourceManager.GetSourceKeys(Convert.ToInt32(sourceId), 0))
            {
                var source = _reportSourceManager.GetSource(sourceKey.SourceId);
                queryString = ReplaceFields(source.Query, sourceId, items);
                joinKeys.Add(sourceKey.Key.ToString(), sourceKey.JoinField);
            }

            List<string> stack = stacks[sourceId];
            string res = "";

            if (!built.Contains(sourceId))
            {
                if (stack[0] != sourceId)
                {
                    string prevKey = stack[stack.Count - 2];
                    if (prevKey == skip)
                    {
                        prevKey = stack[stack.Count - 3];
                    }

                    if (!built.Contains(prevKey))
                    {
                        res += BuildQuery(prevKey, stacks, items, built, avKeys, sourceId);
                    }
                }

                built.Add(sourceId);

                if (avKeys.Count == 0)
                    res += " (";
                else
                    res += " Join (";

                string keyJoin = " on ";
                res += queryString;

                res += ") K" + sourceId;

                foreach (string key in myItems[sourceId].Links)
                {
                    if (avKeys.ContainsKey(key))
                    {
                        res += keyJoin;

                        res += avKeys[key] + " = K" + sourceId + "." + joinKeys[key];

                        keyJoin = " AND ";
                    }
                    else
                    {
                        avKeys.Add(key, "K" + sourceId + "." + joinKeys[key]);
                    }
                }
            }
            return res;
        }
        private string BuildSQuery(MyItem[] tData, SortOrderInfo[] sort, string fieldSource, string fieldKey)
        {
            // If the tData object already has a QueryString, return that
            if (tData.Length == 1 && !string.IsNullOrWhiteSpace(tData[0].QueryString))
            {
                return tData[0].QueryString;
            }

            var avFields = new Dictionary<string, List<string>>();
            if (string.IsNullOrEmpty(fieldSource))
            {
                foreach (var sourceField in _reportSourceManager.GetAllReportFields())
                {
                    string sourceId = sourceField.SourceId.ToString();
                    string fieldName = sourceField.Name;
                    if (ShowColumns(sourceId, tData))
                    {
                        if (!avFields.ContainsKey(sourceId)) avFields.Add(sourceId, new List<string>());
                        avFields[sourceId].Add(fieldName);
                    }
                }
            }
            else
            {
                string sourceId = fieldSource;
                if (!avFields.ContainsKey(sourceId)) avFields.Add(sourceId, new List<string>());

                foreach (var sourceKey in _reportSourceManager.GetSourceKeys(Convert.ToInt32(fieldSource), Convert.ToInt32(fieldKey), 0))
                {
                    avFields[sourceId].Add(sourceKey.JoinField);
                }
            }

            foreach (var sourceInput in _reportSourceManager.GetSourceInputs())
            {
                string sourceId = sourceInput.SourceId.ToString();
                foreach (MyItem item in tData)
                {
                    if (item.ID == sourceId)
                    {
                        foreach (ItemInput input in item.Inputs)
                        {
                            if (input.ID == sourceInput.QueryKey && string.IsNullOrWhiteSpace(input.Value))
                            {
                                input.Value = sourceInput.Value;
                            }
                        }
                    }
                }
            }

            string queryString = string.Empty;

            if (tData.Length > 1)
            {
                Dictionary<string, List<string>> finalStack = null;
                int lastScore = 0;

                foreach (MyItem item in tData)
                {
                    Dictionary<string, List<string>> linkStack = new Dictionary<string, List<string>>();

                    foreach (MyItem sitem in tData)
                    {
                        List<string> stack = null;
                        BuildLnkStack(tData, sitem, item, new List<string>(), out stack);
                        linkStack.Add(sitem.ID, stack);
                    }

                    int newScore = GetLinkedScore(tData, linkStack);

                    if (newScore > lastScore)
                    {
                        lastScore = newScore;
                        finalStack = linkStack;
                    }
                }

                if (finalStack != null)
                {
                    List<string> usedFields = new List<string>();
                    List<string> fieldList = new List<string>();
                    foreach (string key in finalStack.Keys)
                    {
                        if (avFields.ContainsKey(key))
                        {
                            foreach (string fld in avFields[key])
                            {
                                if (!usedFields.Contains(fld))
                                {
                                    usedFields.Add(fld);
                                    fieldList.Add("K" + key + "." + fld);
                                }
                            }
                        }
                    }

                    if (fieldList.Count == 0) throw new Exception("No Data selected");

                    queryString += "select ";

                    bool nfst = false;
                    foreach (string fld in fieldList)
                    {
                        if (nfst) queryString += ", ";
                        queryString += fld;
                        nfst = true;
                    }

                    queryString += " from";

                    Dictionary<string, string> avKeys = new Dictionary<string, string>();
                    List<string> built = new List<string>();
                    foreach (string key in finalStack.Keys)
                    {
                        queryString += BuildQuery(key, finalStack, tData, built, avKeys, "");
                    }

                    if (sort != null && sort.Length > 0)
                    {
                        nfst = false;
                        queryString += " ORDER BY ";
                        foreach (SortOrderInfo so in sort)
                        {
                            foreach (string key in finalStack.Keys)
                            {
                                if (avFields.ContainsKey(key))
                                {
                                    foreach (string fld in avFields[key])
                                    {
                                        if (fld.ToLower().Trim() == so.Field.ToLower().Trim())
                                        {
                                            if (nfst) queryString += ", ";
                                            queryString += "K" + key + "." + fld.Trim() + " " + so.Order;
                                            nfst = true;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    queryString += " GROUP BY ";

                    nfst = false;
                    foreach (string fld in fieldList)
                    {
                        if (nfst) queryString += ", ";
                        queryString += fld;
                        nfst = true;
                    }
                }

            }
            else if (tData.Length == 1)
            {
                if (!avFields.ContainsKey(tData[0].ID)) throw new Exception("No Data selected");
                if (avFields[tData[0].ID].Count == 0) throw new Exception("No Data selected");

                var source = _reportSourceManager.GetSource(Convert.ToInt32(tData[0].ID));

                queryString += "select ";

                bool nfst = false;
                foreach (string fld in avFields[tData[0].ID])
                {
                    if (nfst)
                    {
                        queryString += ", ";
                    }

                    queryString += fld;
                    nfst = true;
                }

                queryString += " from (";
                queryString += ReplaceFields(source.Query, tData[0].ID, tData);
                queryString += ") X";


                if (sort != null && sort.Length > 0)
                {
                    nfst = false;
                    queryString += " ORDER BY ";
                    foreach (SortOrderInfo so in sort)
                    {

                        foreach (string fld in avFields[tData[0].ID])
                        {
                            if (fld.ToLower().Trim() == so.Field.ToLower().Trim())
                            {
                                if (nfst) queryString += ", ";
                                queryString += fld.Trim() + " " + so.Order;
                                nfst = true;
                            }
                        }

                    }
                }
            }

            return queryString;
        }

        private SqlDataType ToSqlDataType(string sqlDataType)
        {
            switch (sqlDataType.ToLowerInvariant())
            {
                case "datetime":
                    return SqlDataType.DateTime;
                case "varchar":
                case "nvarchar":
                    return SqlDataType.String;
                case "int":
                    return SqlDataType.Integer;
                case "float":
                    return SqlDataType.Float;
                case "bit":
                    return SqlDataType.Boolean;
                default:
                    return SqlDataType.String;
            }
        }


        private string ReplacSettingofVariables(string query, string input, string replaceChars)
        {
            string output = query;
            string[] parameters = replaceChars.Split('|');

            string[] inputSplit = input.Split(';');

            foreach (var parameter in parameters)
            {
                if (parameter.Length > 0)
                {
                    string[] keyValue = parameter.Split(':');
                    string newStr = "set @" + keyValue[0] + "=" + keyValue[1];
                    var index = Array.FindIndex(inputSplit, row => row.Contains("@" + keyValue[0]));
                    inputSplit[index] = "\n" + newStr;
                }

            }
            string newInput = string.Empty;
            foreach (var item in inputSplit)
            {
                newInput += item + ";";
            }
            newInput = "--beginset" + newInput + "--endset";

            if (input != null)
            {
                output = query.Replace("--beginset" + input + "--endset", newInput);
            }
            return output;
        }

        private string FirstCharToUpper(string s)
        {
            // Check for empty string.  
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }
            // Return char and concat substring.  
            return char.ToUpper(s[0]) + s.Substring(1);
        }
        public QueryResult GetReportDetails(int recordnumber, int maxRowCount = 0, string replaceChars = "")
        {
            var query = GetReportQuery(recordnumber);
            query = query.ToLower();
            replaceChars = replaceChars.ToLower();

            if (!query.Contains("bovisible"))
            {
                var result = new QueryResult()
                {
                    Columns = new List<ColumnInfo>(),
                    Rows = new List<List<string>>()
                };
                List<string> row = new List<string>();
                row.Add("Permission Denied");
                result.Columns.Add(new ColumnInfo()
                {
                    ColumnName = "Error",
                    DataType = ToSqlDataType("")
                });
                result.Rows.Add(row);
                return result;
            }

            if (replaceChars != null)
            {
                if (replaceChars.Trim().Length > 0)
                {

                    string beginSet = "";
                    //  First Get Data in BEGINSET & ENDSET
                    int istartIndex = query.IndexOf("--beginset");
                    int iendIndex = query.IndexOf("--endset");
                    if (istartIndex > 0 && iendIndex > 0)
                    {
                        beginSet = query.Substring(istartIndex, iendIndex - istartIndex);
                        beginSet = beginSet.Replace("--beginset", "");
                        query = ReplacSettingofVariables(query, beginSet, replaceChars);
                    }
                }
            }

            query = $"SET ROWCOUNT {maxRowCount}; {query}; SET ROWCOUNT 0;";
            try
            {
                using (var dbConnection = new System.Data.SqlClient.SqlConnection(_dataService.GetClientConnectionString().Result))
                {
                    var result = new QueryResult()
                    {
                        Columns = new List<ColumnInfo>(),
                        Rows = new List<List<string>>()
                    };

                    var reader = dbConnection.ExecuteReader(query, commandTimeout: CommandTimeoutSeconds);
                    for (int fieldIndex = 0; fieldIndex < reader.FieldCount; fieldIndex++)
                    {
                        string columnName = FirstCharToUpper(reader.GetName(fieldIndex));
                        if (columnName != null)
                        {
                            if (columnName.Contains(" "))
                            {
                                string[] parameters = columnName.Split(' ');
                                for (int index = 0; index < parameters.Length; index++)
                                {
                                    if (parameters[index].Length > 0)
                                    {
                                        parameters[index] = FirstCharToUpper(parameters[index]);
                                    }
                                }
                                columnName = "";
                                foreach (var item in parameters)
                                {
                                    columnName += item + " ";
                                }
                            }
                        }
                        result.Columns.Add(new ColumnInfo()
                        {
                            ColumnName = columnName,
                            DataType = ToSqlDataType(reader.GetDataTypeName(fieldIndex))
                        });
                    }

                    while (reader.Read())
                    {
                        List<string> row = new List<string>();

                        for (int fieldIndex = 0; fieldIndex < reader.FieldCount; fieldIndex++)
                        {
                            string colname = reader.GetName(fieldIndex);
                            row.Add(reader.GetValue(fieldIndex)?.ToString());
                        }

                        result.Rows.Add(row);
                    }

                    return result;
                }

            }
            catch (Exception e)
            {
                var result = new QueryResult()
                {
                    Columns = new List<ColumnInfo>(),
                    Rows = new List<List<string>>()
                };
                List<string> row = new List<string>();
                row.Add(e.Message);
                result.Columns.Add(new ColumnInfo()
                {
                    ColumnName = "Error",
                    DataType = ToSqlDataType("")
                });
                result.Rows.Add(row);
                return result;
            }
        }
    }
}
