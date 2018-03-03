using ChannelsDB.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ChannelsDB.Core.Models.Annotations
{
    public class SiftsReport
    {
        public Dictionary<string, (string, Dictionary<string, string>)> Sifts;

        public SiftsReport(XElement e)
        {
            Sifts = new Dictionary<string, (string, Dictionary<string, string>)>();

            foreach (var item in e.ElementsAnyNS("entity"))
            {
                var chain = item.Attribute("entityId").Value;
                foreach (var segment in item.ElementsAnyNS("segment"))
                {
                    foreach (var residue in segment.ElementsAnyNS("listResidue").First().ElementsAnyNS("residue"))
                    {
                        var uniprotId = residue.ElementsAnyNS("crossRefDb").FirstOrDefault(x => x.Attribute("dbCoordSys").Value == "UniProt")?.Attribute("dbAccessionId")?.Value;

                        if (uniprotId == null) continue;

                        var uniId = residue.ElementsAnyNS("crossRefDb").FirstOrDefault(x => x.Attribute("dbCoordSys")?.Value == "UniProt")?.Attribute("dbResNum")?.Value;
                        var pdbId = residue.ElementsAnyNS("crossRefDb").FirstOrDefault(x => x.Attribute("dbCoordSys")?.Value == "PDBresnum")?.Attribute("dbResNum")?.Value;

                        if (Sifts.ContainsKey(uniprotId)) Sifts[uniprotId].Item2.AddUnique(uniId, pdbId);
                        else Sifts.Add(uniprotId, (chain, new Dictionary<string, string>() { { uniId, pdbId } }));
                    }
                }
            }
        }
    }
}
