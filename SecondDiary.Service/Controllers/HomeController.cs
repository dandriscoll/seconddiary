using Microsoft.AspNetCore.Mvc;

namespace SecondDiary.API.Controllers
{
    [ApiController]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        [Route("{*path:nonfile}")]
        public IActionResult Index()
        {
            // This handles SPA fallback routing
            return File("~/index.html", "text/html");
        }
    }
}
