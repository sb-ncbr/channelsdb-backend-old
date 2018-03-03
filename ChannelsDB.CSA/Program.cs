using ChannelsDB.Core.Models;
using ChannelsDB.Core.PyMOL;
using ChannelsDB.Core.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ChannelsDB.CSA
{
    class Program
    {
        private static string results = @"D:\Computations\Results";
        private static string final = @"D:\Computations\Final";
        private static string MOLE = @"D:\MOLE.API\Software\MOLE\mole2.exe";
        private static string PyMOL = @"C:\Program Files\PyMOL\PyMOL\PyMOL.exe";

        static void Main(string[] args)
        {
            Directory.SetCurrentDirectory(results);
            CSA csa = new CSA("CSA.txt");
            object locker = new object();
            var i = 0;


            //var map = ParseMapping("pdbtosp.txt");

            Parallel.ForEach(csa.Database, new ParallelOptions() { MaxDegreeOfParallelism = 16 }, x =>
            {
                try
                {
                    if (!File.Exists(Path.Combine()))
                    {
                        ComputeChannels(x);
                        GenerateFigures(x.Key);
                    }

                    lock (locker)
                    {
                        i++;
                        Console.WriteLine(i);
                    }
                }
                catch (Exception)
                {
                    File.AppendAllText("errorlog.log", x.Key + "\n");
                }
            });
        }

        private static void GenerateFigures(string id)
        {
            Directory.CreateDirectory(Path.Combine(final, id));

            Dictionary<string, MoleReport> computations = new Dictionary<string, MoleReport>();
            StringBuilder sb = new StringBuilder();


            foreach (var item in Directory.GetDirectories(Path.Combine(results, id)))
            {
                var rep = JsonConvert.DeserializeObject<MoleReport>(File.ReadAllText(Path.Combine(item, "json", "data.json")));
                computations.Add(Path.GetFileName(item), rep);
            }

            var topActiveSite = computations.OrderByDescending(x => x.Value.Channels.Tunnels.Count()).First();

            if (topActiveSite.Value.Channels.Tunnels.Count() < 1) return;

            File.WriteAllText(Path.Combine(final, id, $"{id}_{topActiveSite.Key}.py"), topActiveSite.Value.Channels.ReportToDownload("pymol", ""));

            var resultPath = Path.Combine(final, id, id + ".py");




            PyMOL p = new PyMOL(PyMOL);

            var script =
            p.GenerateVisualizationScript(
                Path.Combine(@"D:\databases\PDB\bio_assemblies\complete", id + ".cif"),
                Path.Combine(final, id, $"{id}_{topActiveSite.Key}.py"),
                Path.Combine(final, id, $"{id}.png")
                );

            File.WriteAllLines(resultPath, script);

            p.MakePicture(resultPath);         
        }




        private static void ComputeChannels(KeyValuePair<string, List<ActiveSite>> entry)
        {
            for (int i = 0; i < entry.Value.Count; i++)
            {
                Directory.CreateDirectory(Path.Combine(entry.Key, i.ToString()));
                var xml = BuildXML(entry, i);

                ProcessStartInfo info = new ProcessStartInfo()
                {
                    FileName = MOLE,
                    Arguments = xml,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                Process.Start(info).WaitForExit();

            }



        }

        private static string BuildXML(KeyValuePair<string, List<ActiveSite>> entry, int id)
        {
            var root = new XElement("Tunnels");
            var input = new XElement("Input",
                Path.Combine(@"D:\databases\PDB\bio_assemblies\complete", entry.Key + ".cif"));
            var WD = new XElement("WorkingDirectory", Path.Combine(results, entry.Key, id.ToString()));

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


            var origins = BuildOriginElement(entry.Value.ElementAt(id), "Origin");

            root.Add(input);
            root.Add(WD);
            root.Add(nonActive);
            root.Add(par);
            root.Add(export);
            root.Add(origins);


            var path = Path.Combine(results, entry.Key, id.ToString(), "input.xml");

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