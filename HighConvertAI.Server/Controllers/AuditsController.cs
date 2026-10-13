using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HighConvertAI.Server.Services.Abstractions;

namespace HighConvertAI.Server.Controllers
{
    [ApiController]
    [Route("api/audits")]
    public class AuditsController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IAuditService _auditService;
        public AuditsController(IAuthService authService,IAuditService auditService)
        {
            _authService = authService;
            _auditService = auditService;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("get-all-audits")]
        public async Task<IActionResult> GetAllAudits()
        {
            var result = await _auditService.GetAllAudits();
            return Ok(result);
        }

        [Authorize]
        [HttpGet("get-audits-by-user-id")]
        public async Task<IActionResult> GetAuditsByUserId()
        {
            var userId = _authService.GetCurrentUserId();
            var result = await _auditService.GetAuditsByUserId(userId!.Value);
            return Ok(result);
        }
            
        [HttpGet("get-audit-by-id")]
        public async Task<IActionResult> GetAuditById(int id)
        {
            var userId = _authService.GetCurrentUserId();
            var audit = await _auditService.GetSecuredAuditById(id, userId);
            return Ok(audit);
        }
    }
}
