using ChannelsDB.Core.Utils;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ChannelsDB.Core.Models.Annotations
{
    public class SiftsReport
    {
        public Dictionary<string, (string, Dictionary<string, string>)> Sifts { get; set; } = new Dictionary<string, (string, Dictionary<string, string>)>();

        /*
        public SiftsReport(JToken json) {
            foreach (var item in json.Children())
            {
                var uniprotId = ((JProperty)item).Name;
                var child = item.Children();
                foreach (var mapping in item.Children()["mappings"].Children())
                {
                    var chainID = mapping["struct_asym_id"];
                    var uStart = mapping["unp_start"];
                    var uEnd = mapping["unp_end"];
                    var start = mapping["start"];
                    var pStart = mapping["start"]["author_residue_number"];
                    var pEnd = mapping["end"]["author_residue_number"];
                }
            }
        }*/

        public SiftsReport(XElement e)
        {
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

    public class SiftsEntry {
        public string UniProtId { get; set; }
        public int MyProperty { get; set; }
    }

    public class Mapping {
        public string Chain { get; set; }

    }
}
