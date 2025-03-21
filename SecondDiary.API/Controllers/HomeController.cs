using Microsoft.AspNetCore.Mvc;

namespace SecondDiary.API.Controllers
{
    [ApiController]
    [Route("/")]  // Explicitly set the root path
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok("Welcome to SecondDiary!");
        }
    }
}
