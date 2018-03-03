using ChannelsDB.Core.Models;
using ChannelsDB.Core.PyMOL;
using ChannelsDB.Core.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ChannelsDB.Core.Utils.Extensions;

namespace ChannelsDB.Update
{
    class Program
    {
        private static Config c = null;
        private static Dictionary<string, object> results;
        private static APIResult r = new APIResult();


        static void Main(string[] args)
        {

            c = JsonConvert.DeserializeObject<Config>(File.ReadAllText(args[0]));

            results = new Dictionary<string, object>();

            int processed = 0;
            var pictures = new List<string>();
            var excluded = File.ReadAllLines(c.ExcludedEntries);

            
            Console.WriteLine("Looping over database content");

            var console = Console.CursorTop + 1;
            foreach (var dir in Directory.GetDirectories(c.DbRepository))
            {
                foreach (var id in Directory.GetDirectories(dir))
                {
                    try
                    {
                        CheckDatabaseContent(id);

                        if (!File.Exists(Path.Combine(id, Path.GetFileName(id) + ".png")))
                        {
                            pictures.Add(Path.GetFileName(id));
                        }
                    }
                    catch (Exception e) {
                        File.AppendAllText("err.log", $"{id} {e.Message}\n");
                    }
                }
                Console.SetCursorPosition(0, console);
                Console.WriteLine($"Processed {processed++}");
            }

            File.WriteAllText(Path.Combine(c.ApiRoot, "statistics.json"), JsonConvert.SerializeObject(r));
            File.WriteAllText(Path.Combine(c.ApiRoot, "db_content.json"), JsonConvert.SerializeObject(results));                       

            pictures = pictures.Where(x => !excluded.Contains(x)).ToList();
            Console.WriteLine($"Generating {pictures.Count()} pictures");

            MakePictures(pictures);
        }

        public static void CheckDatabaseContent(string archive)
        {
            var id = Path.GetFileNameWithoutExtension(archive);
            archive = Path.Combine(archive, "data.zip");

            var authors = ChannelsCount(JsonConvert.DeserializeObject<MoleReport>(GetZippedFileFromArchive(archive, "authors.json")));
            var csa = ChannelsCount(JsonConvert.DeserializeObject<MoleReport>(GetZippedFileFromArchive(archive, "csa.json")));
            var pores = ChannelsCount(JsonConvert.DeserializeObject<MoleReport>(GetZippedFileFromArchive(archive, "pores.json")));
            var cofactors = ChannelsCount(JsonConvert.DeserializeObject<MoleReport>(GetZippedFileFromArchive(archive, "cofactors.json")));


            results.Add(Path.GetFileNameWithoutExtension(id).ToLower(), new
            {
                counts = new int[] { authors, csa, pores, cofactors }
            });

            if (authors > 0) r.Reviewed++;
            if (csa > 0) r.CSA++;
            if (pores > 0) r.Pores++;
            if (cofactors > 0) r.Cofactors++;

            r.Total++;
        }


        public static int ChannelsCount(MoleReport c)
        {
            if (c?.Channels == null) return 0;
            return c.Channels.MergedPores.Length + c.Channels.Paths.Length + c.Channels.Pores.Length + c.Channels.Tunnels.Length;
        }



        public static void MakePictures(IEnumerable<string> pictures)
        {
            PyMOL p = new PyMOL(c.PyMOL);
            var obj = new object();
            int processed = 1;
            var position = Console.CursorTop + 1;

            Parallel.ForEach(pictures, new ParallelOptions() { MaxDegreeOfParallelism = 4 }, item =>
             {
             try
             {
                 var id = Guid.NewGuid().ToString();
                 //if (File.Exists(Path.Combine(c.DbRepository, item.Substring(1, 2), item, $"{item}.png"))) return;

                 lock (obj)
                 {
                     Console.SetCursorPosition(1, position);
                     Console.WriteLine($"Processed {processed++}");
                 }
                 StringBuilder sb = new StringBuilder();
                 var t = ExtractTunnels(Path.Combine(c.DbRepository, item.Substring(1, 2), item, "data.zip"));
                 PyMolExporter export = new PyMolExporter(sb);
                 export.AddTunnels(t);

                 File.WriteAllText($"pymol_{id}.py", sb.ToString());

                 var script = p.GenerateVisualizationScript(Path.Combine(c.PdbRepository, $"{item}.cif"), $"pymol_{id}.py", Path.Combine(c.DbRepository, item.Substring(1, 2), item, $"{item}.png"));
                 File.WriteAllLines($"{id}.py", script);
                 p.MakePicture($"{id}.py");
                 File.Delete($"{id}.py");
                 File.Delete($"pymol_{id}.py");
                 }
                 catch (Exception e)
                 {
                     File.AppendAllText("err.log", $"{item}    {e.Message}\n");
                 }
             });
        }

        private static List<Tunnel> ExtractTunnels(string zip)
        {
            var order = new string[] { "authors.json", "pores.json", "cofactors.json", "csa.json" };
            MoleReport r = null;
            foreach (var item in order)
            {
                var json = GetZippedFileFromArchive(zip, item);
                if (json != "{}")
                {
                    r = JsonConvert.DeserializeObject<MoleReport>(json);
                    var tunnels = r.Channels.ToTunnelsArray();
                    if (tunnels.Count > 0) return tunnels;
                }
            }

            return new List<Tunnel>();
        }
    }
}