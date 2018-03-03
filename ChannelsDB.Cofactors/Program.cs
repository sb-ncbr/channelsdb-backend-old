using ChannelsDB.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using static ChannelsDB.Core.Utils.Extensions;
namespace ChannelsDB.Cofactors
{
    /// <summary>
    /// Application for updating cofactor entries in the ChannelsDB environment
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Config c = JsonConvert.DeserializeObject<Config>(File.ReadAllText(args[0]));
            var i = 0;
            var added = 0;

            var obj = new object();
            
            Console.WriteLine("Parsing metadata");
            Dictionary<string, IEnumerable<string>> metadata = GetMetadata(c);

            Console.WriteLine("Resolving entries to be processed");
            var idsToProcess = GetIdsToProcess(metadata, c);                      
            var zeroChannels = File.Exists("no_channels.txt") ? File.ReadAllLines("no_channels.txt").ToList() : new List<string>();

            idsToProcess = idsToProcess.Where(x => !zeroChannels.Contains(x)).ToArray();


            Console.WriteLine($"{idsToProcess.Count()} Cofactor entries to be processed.");
            File.WriteAllLines($@"{DateTime.Today.Day}{DateTime.Today.Month}{DateTime.Today.Year}_structures.txt", idsToProcess);

            // run calculations
            Parallel.ForEach(idsToProcess, new ParallelOptions() { MaxDegreeOfParallelism = 12 }, item =>
            {
                try
                {
                    Directory.CreateDirectory(Path.Combine("Computations", item));
                    DownloadStructure(item);
                    var str = BuildXML(item,c);


                    ProcessStartInfo info = new ProcessStartInfo()
                    {
                        FileName = c.Mole,
                        Arguments = str,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };

                    Process.Start(info).WaitForExit();

                    lock (obj) { Console.WriteLine(++i); }
                }
                catch (Exception e)
                {
                    lock (obj) { File.AppendAllText("errors.log", $"{item}   {e.Message}\n"); }
                }
            });

            
            foreach (var item in Directory.GetDirectories("Computations"))
            {
                var id = Path.GetFileNameWithoutExtension(item);
                var path = Path.Combine("Computations", id, "json", "data.json");

                if (!File.Exists(path))
                {
                    zeroChannels.Add(id);
                    continue;
                }

                var channels = JsonConvert.DeserializeObject<MoleReport>(File.ReadAllText(path));

                if (channels.Channels.Tunnels.Count() > 0)
                {
                    added++;

                    Directory.CreateDirectory(Path.Combine(c.DbLocation, id.Substring(1, 2), id));
                    ChannelsDB.Core.Utils.Extensions.CreateArchiveFromFile(Path.Combine(c.DbLocation, id.Substring(1, 2), id, "data.zip"),
                                                                           Path.Combine(item, "json", "data.json"),
                                                                           "cofactors.json");
                }
                else zeroChannels.Add(id);
            }

            Console.WriteLine($"New entries in the database: {added}");
            File.WriteAllLines("no_channels.txt", zeroChannels);

            //clean the mess
            Directory.Delete("Computations", true);
            Directory.GetFiles("Inputs").ToList().ForEach(x => File.Delete(x));

        }

        private static void DownloadStructure(string item)
        {
            using (HttpClient c = new HttpClient())
            {
                var config = c.GetStringAsync($"http://www.ebi.ac.uk/pdbe/static/entry/download/{item}-assembly.xml").Result;
                var xml = XDocument.Parse(config);
                var id = xml.Root.Elements("assembly").First(x => x.Attribute("prefered").Value.Equals("True")).Attribute("id").Value;

                DownloadFile($@"http://coords.litemol.org/{item}/assembly?id={id}", Path.Combine("Inputs", $"{item}.cif"));
            }
        }

        private static string BuildXML(string id, Config c)
        {
            var nonActive = new XElement("NonActiveParts");
            var root = new XElement("Tunnels");
            var input = new XElement("Input", Path.Combine(@"Inputs", $"{id}.cif"));
            var WD = new XElement("WorkingDirectory", Path.Combine(@"Computations", id));

            var par = new XElement("Params",
                new XElement("Cavity",
                    new XAttribute("ProbeRadius", 5),
                    new XAttribute("InteriorThreshold", 1.4),
                    new XAttribute("IgnoreHETAtoms", 1),
                    new XAttribute("IgnoreHydrogens", 1)),
                new XElement("Tunnel",
                    new XAttribute("BottleneckRadius", 1.25),
                    new XAttribute("BottleneckTolerance", 1.0),
                    new XAttribute("FilterBoundaryLayers", "True"),
                    new XAttribute("SurfaceCoverRadius", 10.0),
                    new XAttribute("MinTunnelLength", 15.0),
                    new XAttribute("MaxTunnelSimilarity", 0.7)));

            var export = new XElement("Export",
                new XElement("Formats",
                    new XAttribute("ChargeSurface", "0"),
                    new XAttribute("PyMol", "0"),
                    new XAttribute("PDBProfile", "0"),
                    new XAttribute("VMD", "0"),
                    new XAttribute("Chimera", "0"),
                    new XAttribute("CSV", "0"),
                    new XAttribute("JSON", "1")),
                 new XElement("Types",
                    new XAttribute("Cavities", "0"),
                    new XAttribute("Tunnels", "1"),
                    new XAttribute("PoresAuto", "0"),
                    new XAttribute("PoresMerged", "0"),
                    new XAttribute("PoresUser", "0")),
                 new XElement("PyMol",
                    new XAttribute("SurfaceType", "Spheres")));



            var origins = new XElement("Origins", new XElement("Origin", new XElement("Query", $"Or({c.CofactorQueries.Values.Aggregate((a, b) => a + "," + b)})")));

            root.Add(input);
            root.Add(WD);
            root.Add(nonActive);
            root.Add(par);
            root.Add(export);
            root.Add(origins);


            var xml = Path.Combine("Computations", id, "input.xml");

            using (TextWriter writer = File.CreateText(xml))
            {
                root.Save(writer);
            }

            return xml;
        }



        private static string[] GetIdsToProcess(Dictionary<string, IEnumerable<string>> metadata, Config config)
        {
            string[] dbContent;
            using (HttpClient c = new HttpClient())
            {
                var str = c.GetStringAsync(@"http://webchem.ncbr.muni.cz/API/ChannelsDB/Content").Result;
                dbContent = JsonConvert.DeserializeObject<Dictionary<string, EntryChannels>>(str).Where(x => x.Value.Counts[3] > 0).Select(x => x.Key).ToArray();
            }
            

            var pdbsWithCofactors = metadata.Where(x => x.Value.Any(y => config.Ligands.Contains(y))).Select(x => x.Key);
            return pdbsWithCofactors.Where(x => !dbContent.Contains(x)).ToArray();
        }




        private static Dictionary<string, IEnumerable<string>> GetMetadata(Config c)
        {
            var results = new Dictionary<string, IEnumerable<string>>();
            var root = XDocument.Load(c.PDBMetadataFile).Root;


            foreach (var item in root.Elements("IndexEntry"))
            {
                results.Add(item.Element("Entry").Attribute("FilenameId").Value, item.Element("Properties").Attribute("ResidueTypes").Value.Split(new string[] { "¦" }, StringSplitOptions.RemoveEmptyEntries));
            }

            return results;
        }
    }

    class EntryChannels {
        public int[] Counts { get; set; }
    }
}