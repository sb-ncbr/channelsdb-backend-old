using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace ChannelsDB.API.Controllers
{
    [Route("[controller]")]
    public class DownloadController : Controller
    {
        private readonly Config config;
        private DbManager m = null;

        public DownloadController(IOptions<Config> optionsAccessor)
        {
            config = optionsAccessor.Value;
            m = new DbManager(config);

        }


        [HttpGet("{pdbId}")]
        public async Task<ActionResult> Figure(string pdbId, string type = "zip")
        {
            var array = new byte[0];

            switch (type)
            {
                case "png":
                    var bytes = await m.GetPicture(pdbId);
                    return File(bytes, "image/png", $"{pdbId}.png");
                case "zip":
                    array = m.GetDownloadData(pdbId);
                    return File(array, "application/zip", $"ChannelsDB_{pdbId}.zip");
                case "pdb":
                    array = m.GetPDB(pdbId);
                    return File(array, "text/plain", $"{pdbId}_report.pdb");
                case "py":
                    array = m.GetPyMol(pdbId);
                    return File(array, "text/plain", $"{pdbId}_report.py");
                case "json":
                    var result = m.GetProteinData(pdbId);
                    return new JsonResult(result);
                default:
                    return NotFound();
            }
        }
    }
}
