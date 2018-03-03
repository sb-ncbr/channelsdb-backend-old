using ChannelsDB.Core.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;

namespace ChannelsDB.API.Controllers
{
    [Route("[controller]")]
    [Produces("application/json")]
    public class AssemblyController : Controller
    {
        private readonly Config config;

        public AssemblyController(IOptions<Config> optionsAccessor)
        {
            config = optionsAccessor.Value;
        }

        [HttpGet("{pdbID}")]
        public ContentResult Get(string pdbID)
        {

            string id = "1";
            try
            {
                using (HttpClient c = new HttpClient())
                {
                    var json = c.GetStringAsync($"http://www.ebi.ac.uk/pdbe/api/pdb/entry/summary/{pdbID}").Result;
                    id = (string) JObject.Parse(json).Value<JObject>()[pdbID][0]["assemblies"].First(x => (bool)x["preferred"])["assembly_id"];                                           
                }
            }
            catch (Exception) { }

            return Content(new { AssemblyID = id }.ToJson(), "application/json");


        }
    }
}
