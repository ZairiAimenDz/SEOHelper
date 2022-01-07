using Microsoft.AspNetCore.Mvc;

namespace SEOHelper.Controllers
{
    public class GoogleAnalyticsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
