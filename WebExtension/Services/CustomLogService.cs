using WebExtension.Repositories;
using System;
using System.Threading.Tasks;

namespace WebExtension.Services
{
    public interface ICustomLogService
    {
        Task SaveLog(int associateId, int orderId, string title, string message, string error, string url, string other, string request, string response);
    }
    public class CustomLogService : ICustomLogService
    {
        private readonly ICustomLogRepository _customLogRepository;
        public CustomLogService(ICustomLogRepository customLogRepository)
        {
            _customLogRepository = customLogRepository ?? throw new ArgumentNullException(nameof(customLogRepository));
        }
        public async Task SaveLog(int associateId, int orderId, string title, string message, string error, string url, string other, string request, string response)
        {
            try
            {
                await _customLogRepository.SaveLog(associateId, orderId, title, message, error, url, other, request, response);
            }
            catch (Exception e)
            {

            }
        }
    }
}
