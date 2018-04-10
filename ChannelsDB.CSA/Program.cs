using ChannelsDB.Core.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ChannelsDB.CSA
{
    class Program
    {
        static void Main(string[] args)
        {
            Config c = JsonConvert.DeserializeObject<Config>(File.ReadAllText(args[0]));
            Directory.CreateDirectory("Computations");
            CSA csa = new CSA("CSA.txt");
            object locker = new object();


            Parallel.ForEach(csa.Database, new ParallelOptions() { MaxDegreeOfParallelism = c.Threads }, x =>
            {
                try
                {
                    if (File.Exists(Path.Combine(c.Structures, $"{x.Key}.cif")))
                    {
                        ComputeChannels(x.Key, x.Value, c);
                        PickResults(x.Key, c);
                    }
                }
                catch (Exception)
                {
                    lock (locker) File.AppendAllText("errorlog.log", x.Key + "\n");
                }
            });
        }



        private static void PickResults(string key, Config c)
        {
            Dictionary<string, MoleReport> results = new Dictionary<string, MoleReport>();

            foreach (var item in Directory.GetDirectories(Path.Combine("Computations", key)))
            {
                var path = Path.Combine(item, "json", "data.json");
                if (File.Exists(path))
                {
                    var channels = JsonConvert.DeserializeObject<MoleReport>(File.ReadAllText(path));
                    results.Add(path, channels);
                }

            }

            var ordered = results
                .Where(x => x.Value.Channels.Tunnels.Count() > 0)
                .OrderByDescending(x => x.Value.Channels.Tunnels.Count());

            var toCopy = ordered.FirstOrDefault();

            if (toCopy.Key != null)
            {
                var archive = Path.Combine(c.ChannelsDB, key.Substring(1, 2), key, "data.zip");
                Console.WriteLine($"{toCopy.Key}   {toCopy.Value.Channels.Tunnels.Count()}");
                Core.Utils.Extensions.AddFileToArchive(archive, toCopy.Key, "csa.json");
            }


        }

        private static void ComputeChannels(string pdbId, List<ActiveSite> activeSites, Config c)
        {
            for (int i = 0; i < activeSites.Count; i++)
            {
                Directory.CreateDirectory(Path.Combine("Computations", pdbId, i.ToString()));
                var xml = BuildXML(pdbId, activeSites, i, c);

                ProcessStartInfo info = new ProcessStartInfo()
                {
                    FileName = c.Mole,
                    Arguments = xml,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                Process.Start(info).WaitForExit();

            }



        }

        private static string BuildXML(string pdbId, List<ActiveSite> activeSites, int id, Config c)
        {
            var root = new XElement("Tunnels");
            var input = new XElement("Input",
                Path.Combine(c.Structures, $"{pdbId}.cif"));
            var WD = new XElement("WorkingDirectory", Path.Combine("Computations", pdbId, id.ToString()));

            var nonActive = new XElement("NonActiveParts",
                new XElement("Query", "HetResidues().Filter(lambda m: m.IsNotConnectedTo(AminoAcids()))"));

            var par = new XElement("Params",
                new XElement("Cavity",
                    new XAttribute("ProbeRadius", 5),
                    new XAttribute("InteriorThreshold", 1.1),
                    new XAttribute("IgnoreHETAtoms", "0"),
                    new XAttribute("IgnoreHydrogens", "1")),
                new XElement("Tunnel",
                    new XAttribute("MinTunnelLength", 15.0),
                    new XAttribute("BottleneckRadius", 1.25),
                    new XAttribute("BottleneckTolerance", 3.0),
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
                    new XAttribute("PoresUser", "0")));


            var origins = BuildOriginElement(activeSites.ElementAt(id), "Origin");

            root.Add(input);
            root.Add(WD);
            root.Add(nonActive);
            root.Add(par);
            root.Add(export);
            root.Add(origins);


            var path = Path.Combine("Computations", pdbId, id.ToString(), "input.xml");

            using (TextWriter writer = File.CreateText(path))
            {
                root.Save(writer);
            }

            return path;
        }



        /// <summary>
        /// Builds Origin element. If null or empty automatic start points are used for calculation.
        /// PatternQuery expression, Residues[] and Points3D[] are all included
        /// </summary>
        /// <param name="o">Origin parameter</param>
        /// <returns>XElement for the MOLE input XML</returns>
        private static XElement BuildOriginElement(ActiveSite aSite, string key)
        {
            var element = new XElement($"{key}s",
                new XElement(key, BuildResiduesElement(aSite.Residues.ToArray())));

            return element;
        }



        /// <summary>
        /// Given the list of residues builds their XML notation
        /// </summary>
        /// <param name="residues"></param>
        /// <returns></returns>
        private static XElement[] BuildResiduesElement(Residue[] residues) =>
            residues.Select(x => new XElement("Residue", new XAttribute("SequenceNumber", x.Id), new XAttribute("Chain", x.Chain))).ToArray();




        private static Dictionary<string, List<string>> ParseMapping(string v)
        {
            var pdbUniProtMapping = new Dictionary<string, List<string>>();

            var pdbId = string.Empty;
            var uniProts = new List<string>();

            var lines = File.ReadAllLines(v).Skip(24);

            foreach (var item in lines)
            {
                var tempUnis = Regex.Matches(item.Substring(30), @"\(.{6,8}\)").Cast<Match>().Select(x => x.Value.Trim(new char[] { '(', ')' }));

                if (Regex.Match(item, @"^\d").Success)
                {
                    pdbUniProtMapping.Add(pdbId, uniProts);

                    uniProts = new List<string>();
                    pdbId = item.Substring(0, 4).ToLower();

                }
                uniProts.AddRange(tempUnis);
            }
            pdbUniProtMapping.Add(pdbId, uniProts);


            pdbUniProtMapping.Remove(string.Empty);
            return pdbUniProtMapping;
        }


    }

}