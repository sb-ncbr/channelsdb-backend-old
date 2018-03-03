using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IO;
using ChannelsDB.API.Utils;
using ChannelsDB.Core.Utils;
using Newtonsoft.Json;
using ChannelsDB.Core.Models;
using ChannelsDB.Core.Models.Annotations;

namespace ChannelsDB.API.Controllers
{
    [Route("[controller]")]    
    public class ComponentController : Controller
    {
        private readonly Config config;
        private DbManager m;

        public ComponentController(IOptions<Config> optionsAccessor)
        {
            config = optionsAccessor.Value;
            this.m = new DbManager(config);
        }


        [HttpGet("{id}")]
        public ActionResult Get(string id)
        {
            if (!m.CheckId(id)) return new ContentResult() { Content = "{}" };
            MoleReport report = null;

            var content = new string[] { DBFiles.Authors, DBFiles.Pores, DBFiles.Cofactor, DBFiles.CSA };

            var zip = Path.Combine(config.LocalPath, id.Substring(1, 2), id, DBFiles.Archive);

            for (int i = 0; i < content.Length; i++)
            {
                report = JsonConvert.DeserializeObject<MoleReport>(Extensions.GetZippedFileFromArchive(zip, content[i]));
                var annotations = JsonConvert.DeserializeObject<TunnelAnnotations[]>(Extensions.GetZippedFileFromArchive(zip, DBFiles.Annotation));

                if (report.Channels.ToTunnelsArray().Count() > 0)
                    return Content(JsonConvert.SerializeObject(ToComponentTunnels(report.Channels.ToTunnelsArray(), annotations, i)), "application/json");
            }


            return Content(System.IO.File.ReadAllText(Path.Combine(config.LocalPath, "tq", "1tqn", "1tqn.json")), "application/json");            
        }

        private dynamic ToComponentTunnels(IEnumerable<Tunnel> ts, TunnelAnnotations[] ann, int i)
        {
            Dictionary<string, string> lookup = new Dictionary<string, string>();

            switch (i)
            {
                case 0:
                    foreach (var item in ann)
                    {
                        if (!lookup.ContainsKey(item.Id)) lookup.Add(item.Id, item.Name);
                    }
                    break;
                case 1:
                    lookup = ts.Select(x => x.Id).Distinct().ToDictionary(x => x, x => "Transmembrane pore");
                    break;
                case 2:
                    lookup = ts.Select(x => x.Id).Distinct().ToDictionary(x => x, x => "CSA channel");
                    break;
                case 3:
                    lookup = ts.Select(x => x.Id).Distinct().ToDictionary(x => x, x => "Cofactor channel");
                    break;
                default:
                    lookup = ts.Select(x => x.Id).Distinct().ToDictionary(x => x, x => "Channel");
                    break;
            }


            List<dynamic> results = new List<dynamic>();

            foreach (var item in ts)
            {
                var tunnel = new
                {
                    ID = item.Id,
                    Type = i,
                    Name = lookup[item.Id],
                    Properties = new
                    {
                        Polarity = item.Layers.LayerWeightedProperties.Polarity,
                        Hydropathy = item.Layers.LayerWeightedProperties.Hydropathy,
                        Mutability = item.Layers.LayerWeightedProperties.Mutability,
                        Hydrophobicity = item.Layers.LayerWeightedProperties.Hydrophobicity,
                        Charge = item.Properties.Charge,
                        NumPositives = item.Properties.NumPositives,
                        NumNegatives = item.Properties.NumNegatives
                    },
                    Layers = item.Layers.LayersInfo.Select(y => new
                    {
                        MinRadius = y.LayerGeometry.MinRadius,
                        MinFreeRadius = y.LayerGeometry.MinFreeRadius,
                        StartDistance = y.LayerGeometry.StartDistance,
                        EndDistance = y.LayerGeometry.EndDistance,
                        LocalMinimum = y.LayerGeometry.LocalMinimum,
                        Properties = y.Properties,
                        Residues = ToResidues(y.Residues)
                    }),
                    Lining = ToResidues(item.Layers.ResidueFlow),
                    Length = item.Profile.Last().Distance,
                    Bottleneck = ToBottleneckLayer(item.Layers)
                };

                results.Add(tunnel);
            }
            return results;
        }



        private List<Residue> ToResidues(string[] s)
        {
            var result = new List<Residue>();

            foreach (var item in s)
            {
                var tempR = new Residue(item);
                if (result.Contains(tempR))
                {
                    result.First(x => x.Equals(tempR)).ChangeState(item);

                }
                else result.Add(tempR);
            }
            return result;
        }




        private dynamic ToBottleneckLayer(Layers l)
        {
            var min = l.LayersInfo.Min(x => x.LayerGeometry.MinRadius);
            var btnck = l.LayersInfo.First(x => x.LayerGeometry.MinRadius == min);

            return new
            {
                MinRadius = btnck.LayerGeometry.MinRadius,
                MinFreeRadius = btnck.LayerGeometry.MinFreeRadius,
                StartDistance = btnck.LayerGeometry.StartDistance,
                EndDistance = btnck.LayerGeometry.EndDistance,
                LocalMinimum = true,
                Properties = btnck.Properties,
                Residues = ToResidues(btnck.Residues)
            };
        }

    }
}