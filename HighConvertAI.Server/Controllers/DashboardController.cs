using HighConvertAI.Server.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HighConvertAI.Server.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("get-admin-overview")]
        public async Task<IActionResult> GetAdminOverview()
        {
            var result = await _dashboardService.GetAdminOverview();
            return Ok(result);
        }

    }
}
