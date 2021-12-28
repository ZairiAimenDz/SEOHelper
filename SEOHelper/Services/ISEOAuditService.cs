
using SEOHelper.Models;

namespace SEOHelper.Service
{
    public interface ISEOAuditService
    {
        Task<WebsiteAuditResult> AllPageAudit(string URL,string PrimaryKeyword,string[]? Keywords);
        Task<PageAuditResult> OnePageAudit(string URL, string PrimaryKeyword,string[]? Keywords);
    }
}