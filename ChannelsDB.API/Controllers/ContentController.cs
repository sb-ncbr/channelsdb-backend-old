using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ChannelsDB.API.Controllers
{    
    [Route("[controller]")]
    public class ContentController : Controller
    {
        private readonly Config config;

        public ContentController(IOptions<Config> optionsAccessor)
        {
            config = optionsAccessor.Value;
        }

        [HttpGet]
        public ActionResult Get() {
            return Content(System.IO.File.ReadAllText("db_content.json"), "application/json");
        }
    }
}