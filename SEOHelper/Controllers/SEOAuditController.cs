using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SEOHelper.Models;
using SEOHelper.Service;

namespace SEOHelper.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SEOAuditController : ControllerBase
    {
        private readonly ISEOAuditService auditService;

        public SEOAuditController(ISEOAuditService service)
        {
            this.auditService = service;
        }

        [HttpGet("Page")]
        public async Task<IActionResult> GetPageAudit([FromQuery]string[] URLs
            ,[FromQuery]string PrimaryKeyword
            ,[FromQuery]string[]? Keywords)
        {
            List<PageAuditResult> results = new();
            foreach (var url in URLs)
            {
                results.Add(await auditService.OnePageAudit(url, PrimaryKeyword, Keywords));
            }
            return Ok(results);
        }

        [HttpGet("Site")]
        public async Task<IActionResult> GetSiteAudit([FromQuery]string URL
            ,[FromQuery]string PrimaryKeyword
            ,[FromQuery]string[]? Keywords)
        {
            return Ok(await auditService.AllPageAudit(URL,PrimaryKeyword,Keywords));
        }
    }
}
