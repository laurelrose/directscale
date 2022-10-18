using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebExtension.Services.DailyRun;

namespace WebExtension.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestApiController : ControllerBase
    {
        private readonly IDailyRunService _dailyRunService;

        public TestApiController(IDailyRunService dailyRunService)
        {
            _dailyRunService = dailyRunService;
        }

        [HttpGet]
        [Route("TestApi")]
        public IActionResult TestApi()
        {
            _dailyRunService.GetAssociateStatuses();
            return Ok();
        }
    }
}
