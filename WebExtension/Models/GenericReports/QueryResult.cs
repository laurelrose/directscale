using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

namespace WebExtension.Models.GenericReports
{

    [JsonConverter(typeof(StringEnumConverter))]
    public enum SqlDataType
    {
        String = 0,
        Integer = 1,
        Float = 2,
        Boolean = 3,
        DateTime = 4
    }
    public enum ReportType
    {
        ReportBuilder = 1,
        SavedQuery = 2
    }
    public class QueryResult
    {
        [JsonProperty("Columns")]
        public List<ColumnInfo> Columns { get; set; }
        [JsonProperty("Rows")]
        public List<List<string>> Rows { get; set; }
    }
}
