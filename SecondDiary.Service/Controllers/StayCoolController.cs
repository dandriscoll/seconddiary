using Microsoft.AspNetCore.Mvc;

namespace SecondDiary.Controllers
{
    [Route("/staycool")]
    [ApiController]
    public class StayCoolController : ControllerBase
    {
        [HttpGet]
        public Task GetAsync()
        {
            return Task.CompletedTask;
        }

        public static void Go(string? baseUrl, ILogger logger)
        {
            if (string.IsNullOrEmpty(baseUrl))
            {
                logger.LogWarning("baseUrl is not set. StayCoolController will not run.");
                return;
            }
                
            Task.Run(async () =>
            {
                HttpClient client = new HttpClient();

                while (true)
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromMinutes(2), CancellationToken.None);
                        await client.GetAsync($"{baseUrl}/staycool");
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Error in StayCoolController: {Message}", e.Message);
                    }
                }
            });
        }
    }
}
