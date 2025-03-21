using Microsoft.AspNetCore.Mvc;

namespace SecondDiary.API.Controllers
{
    [ApiController]
    [Route("/")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            // This will be handled by the SPA fallback
            return File("~/index.html", "text/html");
        }
    }
}
