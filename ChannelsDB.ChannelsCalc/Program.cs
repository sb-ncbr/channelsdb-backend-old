using ChannelsDB.Core.Models.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ChannelsDB.ChannelsCalc
{
    class Program
    {
        static void Main(string[] args)
        {
            Directory.CreateDirectory("Structures");
            var pdbId = "2bg9";
            var otherPdbs = GetOtherPDBs(pdbId);
            DownloadStructures(otherPdbs);
        }





        private static void DownloadStructures(string[] otherPdbs)
        {
            using (HttpClient c = new HttpClient())
            {
                foreach (var item in otherPdbs)
                {
                    var configXML = c.GetStringAsync($"http://www.ebi.ac.uk/pdbe/static/entry/download/{item}-assembly.xml").Result;
                    var xml = XDocument.Parse(configXML);
                    var id = xml.Root.Elements("assembly").First(x => x.Attribute("prefered").Value.Equals("True")).Attribute("id").Value;
                    File.WriteAllText("Structures", c.GetStringAsync($"https://webchem.ncbr.muni.cz/CoordinateServer/{item}/assembly?id={id}").Result);
                }
            }
        }





        private static string[] GetOtherPDBs(string pdbId)
        {
            HashSet<string> set = new HashSet<string>();

            using (HttpClient c = new HttpClient())
            {
                
                var str = c.GetStringAsync($@"http://www.ebi.ac.uk/pdbe/api/mappings/uniprot/{pdbId}");
                var uniprots = ParseUniProts(pdbId, str.Result);


                foreach (var item in uniprots)
                {
                    var pdbs = Regex.Matches(c.GetStringAsync($"http://www.ebi.ac.uk/pdbe/api/mappings/best_structures/{item}").Result, "pdb_id\":.{5}").Cast<Match>().Select(x => x.Value.Substring(9));
                    pdbs.ToList().ForEach(x => set.Add(x));
                }
            }
            return set.ToArray();
        }

        private static string[] ParseUniProts(string pdbId, string s) {
            return ((Dictionary<string, object>)((Dictionary<string, object>)DeserializeToDictionary(s)[pdbId])["UniProt"]).Keys.ToArray();
        }

        

        private static Dictionary<string, object> DeserializeToDictionary(string jo)
        {
            var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(jo);
            var values2 = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> d in values)
            {
                if (d.Value is JObject)
                {
                    values2.Add(d.Key, DeserializeToDictionary(d.Value.ToString()));
                }
                else
                {
                    values2.Add(d.Key, d.Value);
                }
            }
            return values2;
        }
    }
}