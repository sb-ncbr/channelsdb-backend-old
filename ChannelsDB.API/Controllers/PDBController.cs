using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ChannelsDB.Core.Utils;

namespace ChannelsDB.API.Controllers
{
    [Route("[controller]")]    
    public class PDBController : Controller
    {
        private readonly Config config;

        public PDBController(IOptions<Config> optionsAccessor)
        {
            config = optionsAccessor.Value;
        }


        [HttpGet("{id}")]
        public ActionResult Get(string id)
        {
            DbManager m = new DbManager(config);

            if (!m.CheckId(id))
                return Content(
                    new
                    {
                        Error = $"{id} is not processed yet. If you do have annotations, please <a href='mailto:webchemistryhelp@gmail.com?subject=Annotations for {id} entry'>share them.</a>"
                    }.ToJson(), "application/json");                

            return Content(m.GetProteinData(id).ToJson(), "application/json");
        }
    }
}
