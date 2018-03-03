using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ChannelsDB.API.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    public class StatisticsController : Controller
    {
        private readonly Config config;

        public StatisticsController(IOptions<Config> optionsAccessor)
        {
            config = optionsAccessor.Value;
        }

        [HttpGet]
        public ActionResult Get()
        {
            return Content(System.IO.File.ReadAllText("statistics.json"), "application/json");
        }
    }
}