using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using WebExtension.Helper;
using WebExtension.Services;
using WebExtension.Services.TableCreation;

namespace WebExtension.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TableCreationController : ControllerBase
    {
        public static string _className = nameof(TableCreationController);

        private readonly ICustomLogService _customLogService;
        private readonly ITableCreationService _tableCreationService;

        public TableCreationController(
            ICustomLogService customLogService,
            ITableCreationService tableCreationService
        )
        {
            _customLogService = customLogService ?? throw new ArgumentException(nameof(customLogService));
            _tableCreationService = tableCreationService ?? throw new ArgumentException(nameof(tableCreationService));
        }

        [HttpPost]
        [Route("CreateRewardPointCreditInfrastructure")]
        public async Task<IActionResult> CreateRewardPointCreditInfrastructure()
        {
            try
            {
                await _customLogService.SaveLog(0, 0, $"{_className}.CreateRewardPointCreditInfrastructure", "Information", "CreateRewardPointCreditInfrastructure Triggered", "", "", "", CommonMethod.Serialize(DateTime.Now));

                await _tableCreationService.CreateRewardPointCreditInfrastructure();

                return new Responses().OkResult();
            }
            catch (Exception ex)
            {
                await LogErrorAsync($"{_className}.CreateRewardPointCreditInfrastructure", ex);
                return new Responses().ErrorResult(ex);
            }
        }

        private async Task LogErrorAsync(string methodName, Exception ex)
        {
            await _customLogService.SaveLog(0, 0, methodName, "Error", ex.Message, "", "", "", CommonMethod.Serialize(ex));
        }
    }
}
