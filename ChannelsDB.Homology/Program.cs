using ChannelsDB.Core.Models;
using ChannelsDB.Core.PyMOL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ChannelsDB.Homology
{
    class Program
    {
        static void Main(string[] args)
        {
            PyMOL p = new PyMOL(@"C:\Program Files\PyMOL\PyMOL\PyMOL.exe");

            var structures = @"C:\Users\Lukas\Documents\Visual Studio 2017\Projects\Cytochromes\Cytochromes\bin\Debug\Input";

            foreach (var dir in Directory.GetDirectories(@"C:\Users\Lukas\Documents\Visual Studio 2017\Projects\Cytochromes\Cytochromes\bin\Debug\Computations"))
            {
                var json = Path.Combine(dir, "json", "data.json");
                var pymol = Path.Combine(dir, "pymol", "complex.py");
                var report = JsonConvert.DeserializeObject<MoleReport>(File.ReadAllText(json));
                var id = Path.GetFileNameWithoutExtension(dir);


                if (report.Channels.Tunnels.Count() > 0)
                {
                    File.Copy(json, Path.Combine(@"D:\work\_phd\_live\ChannelsDB\_misc\cytochromes\results", id + ".json"));
                    var script = p.GenerateVisualizationScript(Path.Combine(structures, id + ".cif"), pymol, Path.Combine(@"D:\work\_phd\_live\ChannelsDB\_misc\cytochromes\results", id + ".png"));

                    File.WriteAllLines("pymol.py", script);

                    p.MakePicture("pymol.py");

                }

            }


            //var pivot = "1jj2";
            //var ids = File.ReadAll

            //align
            //transfer

            //build
            //compute
            //pack

        }

        private static string BuildXML(string source, string wd, XElement start, XElement stop)
        {
            var root = new XElement("Tunnels");
            var input = new XElement("Input", source);

            var WD = new XElement("WorkingDirectory", wd);

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
                    new XAttribute("MaxTunnelSimilarity", 0.7),
                    new XAttribute("UseCustomExitsOnly", "True")));

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


            var origins = new XElement("Origins", new XElement("Origin", start));
            var exit = new XElement("CustomExits", new XElement("Exit", stop));

            root.Add(input);
            root.Add(WD);

            root.Add(par);
            root.Add(export);
            root.Add(origins);
            root.Add(exit);

            /*
            var path = Path.Combine(results, entry.Key, id.ToString(), "input.xml");

            using (TextWriter writer = File.CreateText(path))
            {
                root.Save(writer);
            }
            */
            return "";
        }
    }
}
