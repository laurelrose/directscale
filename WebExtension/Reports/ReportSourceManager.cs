using System;
using System.Collections.Generic;
using System.Linq;
using WebExtension.Models.GenericReports;
using WebExtension.Repositories;

namespace WebExtension.Reports
{

    public interface IReportSourceManager
    {
        SourceKey[] GetSourceKeys(int sourceID, int subKey);
        SourceKey[] GetSourceKeys(int sourceID, int key, int subKey);
        SourceKey[] GetSourceKeys();
        SourceInput[] GetSourceInputs();
        SourceInput[] GetSourceInputs(int sourceID);
        Source GetSource(int sourceID);
        SourceField[] GetAllReportFields();
        List<ReportKey> GetReportKeys();
        Source[] GetSources();
    }
    public class ReportSourceManager : IReportSourceManager
    {
        private readonly IReportSourceRepository _reportSourceRepository;

        public ReportSourceManager(IReportSourceRepository reportSourceRepository)
        {
            _reportSourceRepository = reportSourceRepository ?? throw new ArgumentNullException(nameof(reportSourceRepository));
        }

        public SourceKey[] GetSourceKeys(int sourceId, int subKey)
        {
            return GetSourceKeys(sourceId, 0, subKey);
        }

        public SourceKey[] GetSourceKeys(int sourceId, int key, int subKey)
        {
            if (key > 0)
            {
                return GetSourceKeys().ToList().FindAll(x => { return x.SourceId == sourceId && x.SubKey == subKey && x.Key == key; }).ToArray();
            }
            else
            {
                return GetSourceKeys().ToList().FindAll(x => { return x.SourceId == sourceId && x.SubKey == subKey; }).ToArray();
            }
        }

        public SourceKey[] GetSourceKeys()
        {
            List<SourceKey> items = new List<SourceKey>();
            foreach (var Source in GetSources())
            {
                foreach (var sourceKey in Source.SourceKeys)
                {
                    sourceKey.SourceId = Source.Id;
                    items.Add(sourceKey);
                }
            }

            List<ReportKey> reportKeys = GetReportKeys();

            foreach (var item in items)
            {
                item.Color = reportKeys.Find(x => x.Key == item.Key).Color;
            }

            return items.ToArray();
        }

        public SourceInput[] GetSourceInputs()
        {
            var res = new List<SourceInput>();

            foreach (var source in GetSources())
            {
                foreach (var input in source.SourceInputs)
                {
                    input.SourceId = source.Id;
                    res.Add(input);
                }
            }

            return res.ToArray();
        }

        public SourceInput[] GetSourceInputs(int sourceId)
        {
            return GetSourceInputs().ToList().FindAll(x => { return x.SourceId == sourceId; }).ToArray();
        }

        public Source GetSource(int sourceId)
        {
            foreach (var source in GetSources())
            {
                if (source.Id == sourceId) return source;
            }

            return null;
        }

        public SourceField[] GetAllReportFields()
        {
            var res = new List<SourceField>();

            foreach (var source in GetSources())
            {
                foreach (var col in source.Columns)
                {
                    res.Add(new SourceField
                    {
                        SourceId = source.Id,
                        Name = col
                    });
                }
            }

            return res.ToArray();
        }

        public List<ReportKey> GetReportKeys()
        {
            return _reportSourceRepository.GetReportKeys();
        }

        public Source[] GetSources()
        {
            Dictionary<int, List<string>> columns = _reportSourceRepository.LoadColumns();
            Dictionary<int, List<SourceKey>> sourceKeys = _reportSourceRepository.LoadSourceKeys();
            Dictionary<int, List<SourceInput>> sourceInputs = _reportSourceRepository.LoadInputs();

            Source[] sources = _reportSourceRepository.GetSources();
            foreach (var source in sources)
            {
                source.Columns = columns.ContainsKey(source.Id) ? columns[source.Id] : new List<string>();
                source.SourceKeys = sourceKeys.ContainsKey(source.Id) ? sourceKeys[source.Id] : new List<SourceKey>();
                source.SourceInputs = sourceInputs.ContainsKey(source.Id) ? sourceInputs[source.Id] : new List<SourceInput>();
            }

            return sources;
        }
    }
}
