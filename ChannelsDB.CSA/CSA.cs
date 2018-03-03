using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ChannelsDB.CSA
{
    public class CSA
    {
        public Dictionary<string, List<ActiveSite>> Database { get; set; }

        public CSA(string pathToCSA)
        {
            Database = new Dictionary<string, List<ActiveSite>>();

            var temp = File.ReadAllLines(pathToCSA).
                Skip(1).
                GroupBy(x => x.Substring(0, 4));

            foreach (var protein in temp)
            {
                Database.Add(protein.Key, new List<ActiveSite>());
                var sites = protein.GroupBy(x => x.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[1]);

                foreach (var site in sites)
                {
                    var activeSite = new ActiveSite();
                    foreach (var residue in site)
                    {
                        Residue r = new Residue(residue);
                        activeSite.Residues.Add(r);
                    }
                    Database[protein.Key].Add(activeSite);
                }
            }
        }
    }

    public class ActiveSite
    {
        public List<Residue> Residues { get; set; }

        public ActiveSite()
        {
            Residues = new List<Residue>();
        }

    }

    public class Residue
    {
        public string Id { get; set; }
        public string Chain { get; set; }

        public Residue(string line)
        {
            var splitted = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            Id = splitted[4];
            Chain = splitted[3];
        }

        public override string ToString()
        {
            return $"{Id} {Chain}";
        }
    }
}
