using System.Collections.Generic;

namespace WebExtension.Models.GenericReports
{
    public class Source
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Query { get; set; }

        public List<SourceKey> SourceKeys { get; set; }
        public List<SourceInput> SourceInputs { get; set; }
        public List<string> Columns { get; set; }
    }
}
