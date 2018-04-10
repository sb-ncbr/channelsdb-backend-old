using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChannelsDB.Cofactors
{
    class Config
    {
        private Dictionary<string, string> _cofactorQueries;
        public string[] Ligands { get; private set; }

        public int Threads { get; set; }
        public string PDBMetadataDirectory { get; set; }
        public string ChannelsDB { get; set; }
        public string Structures { get; set; }
        public string Mole { get; set; }

        public Dictionary<string, string> CofactorQueries
        {
            get
            {
                return _cofactorQueries;
            }
            set
            {
                Ligands = ParseLigands(value);
                _cofactorQueries = value;
            }
        }

        public string PDBMetadataFile
        {
            get
            {
                var files = Directory.GetFiles(PDBMetadataDirectory).Where(x => Path.GetFileNameWithoutExtension(x).StartsWith("index"));
                var count = files.Count();

                return files.First(x => Path.GetFileNameWithoutExtension(x) == $"index_{count}");
            }
        }

        


        public string[] ParseLigands(Dictionary<string, string> p)
        {
            var residues = p.Values.SelectMany(x => Regex.Matches(x, "Residues(.+)").Cast<Match>().Select(y => y.Value));
            return residues.SelectMany(x => Regex.Matches(x, "\'.{1,3}\'").Cast<Match>().Select(y => y.Value.Trim(new char[] { '\'' }))).ToArray();

        }

    }
}
