using System;
using Microsoft.Extensions.Logging;
using WebExtension.Repositories;
using WebExtension.Models.GenericReports;

namespace WebExtension.Services
{

    public interface IGenericReportService
    {
        QueryResult GetReportDetails(int recordnumber, string replaceChars);
    }
    public class GenericReportService : IGenericReportService
    {
        private readonly ILogger<GenericReportService> _logger;
        private readonly IGenericReportRepository _getGenericReportRepository;

        public GenericReportService(ILogger<GenericReportService> logger, IGenericReportRepository getGenericReportRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _getGenericReportRepository = getGenericReportRepository ?? throw new ArgumentNullException(nameof(getGenericReportRepository));
        }

        public QueryResult GetReportDetails(int recordnumber, string replaceChars)
        {
            return _getGenericReportRepository.GetReportDetails(recordnumber, 0, replaceChars);
        }
    }
}
