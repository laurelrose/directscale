using System;
using System.Threading.Tasks;

namespace WebExtension.Services.TableCreation
{
    public interface ITableCreationService
    {
        public Task CreateRewardPointCreditInfrastructure();
    }

    internal class TableCreationService : ITableCreationService
    {
        private readonly ITableCreationRepository _tableCreationRepository;

        public TableCreationService(ITableCreationRepository tableCreationRepository)
        {
            _tableCreationRepository = tableCreationRepository ?? throw new ArgumentNullException(nameof(tableCreationRepository));
        }

        public async Task CreateRewardPointCreditInfrastructure()
        {
            await _tableCreationRepository.CreateRewardPointCreditInfrastructure();
        }
    }
}
