using ChannelsDB.Core.Utils;
using ChannelsDB.Core.Models.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ChannelsDB.API.Controllers
{   
    [Route("[controller]")]
    public class AnnotationsController : Controller
    {
        private readonly Config config;

        public AnnotationsController(IOptions<Config> optionsAccessor)
        {
            config = optionsAccessor.Value;
        }


        [HttpGet("{pdbID}")]
        public ActionResult Get(string pdbID)
        {
            Annotation annotations = null;
            SiftsReport sifts = null;

            using (HttpClient cl = new HttpClient())
            {
                cl.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));

                var xmlString = ChannelsDB.Core.Utils.Extensions.DownloadFTPCompressedGz(@"ftp://ftp.ebi.ac.uk", $@"/pub/databases/msd/sifts/xml/{pdbID}.xml.gz");

                if (String.IsNullOrEmpty(xmlString)) return new ContentResult() { Content = new Annotation().ToJson() };

                var xdoc = XDocument.Parse(xmlString);
                sifts = new SiftsReport(xdoc.Root);

                annotations = new Annotation();


                foreach (var id in sifts.Sifts)
                {
                    try
                    {
                        var uniprotxml = cl.GetStringAsync($@"https://www.ebi.ac.uk/proteins/api/proteins/{id.Key}").Result;
                        var xml = XDocument.Parse(uniprotxml);

                        if (annotations.EntryAnnotations.FirstOrDefault(x => x.UniProtId == id.Key) == null)
                        {
                            var eAn = new UniProtEntryDetails(id.Key, xml.Root);
                            annotations.EntryAnnotations.Add(eAn);
                        }
                        annotations.AddResidues(id.Value, xml.Root);
                        annotations.AddChannelResidues(id.Value, Path.Combine(config.Annotations, $"{id.Key}.json"));
                    }
                    catch (Exception) {
                    }
                }
            }

            return Content(annotations.ToJson(), "application/json");
        }
    }

}

