using System;
using System.Threading.Tasks;

namespace WebExtension.Services.TableCreation
{
    public interface ITableCreationService
    {
        public Task CreateRewardPointCreditTable();
    }

    internal class TableCreationService : ITableCreationService
    {
        private readonly ITableCreationRepository _tableCreationRepository;

        public TableCreationService(ITableCreationRepository tableCreationRepository)
        {
            _tableCreationRepository = tableCreationRepository ?? throw new ArgumentNullException(nameof(tableCreationRepository));
        }

        public async Task CreateRewardPointCreditTable()
        {
            await _tableCreationRepository.CreateRewardPointCreditTable();
        }
    }
}
