using System;
using System.Threading.Tasks;
using DirectScale.Disco.Extension.Services;
using Medallion.Threading.SqlServer;

namespace WebExtension.Services.DistributedLocking
{
    public interface IDistributedLockingService
    {
        Task<IDisposable> CreateDistributedLockAsync(string lockName, TimeSpan? timeSpan = null);
    }

    public class DistributedLockingService : IDistributedLockingService
    {
        private readonly IDataService _dataService;

        public DistributedLockingService(IDataService dataService)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        }

        public async Task<IDisposable> CreateDistributedLockAsync(string lockName, TimeSpan? timeSpan = null)
        {
            timeSpan ??= TimeSpan.FromSeconds(10);

            return await new SqlDistributedLock(lockName, await _dataService.GetClientConnectionString()).TryAcquireAsync(timeSpan.Value);
        }
    }
}
