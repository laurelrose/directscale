using Dapper;
using DirectScale.Disco.Extension.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace WebExtension.Repositories
{
    public interface IAssociateWebRepository
    {
    }
    public class AssociateWebRepository : IAssociateWebRepository
    {
        private readonly IDataService _dataService;

        public AssociateWebRepository(IDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        }
    }
}
