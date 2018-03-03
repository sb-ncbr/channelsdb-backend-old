using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ChannelsDB.Core.Models.Annotations
{
    /// <summary>
    /// Uniprot:
    ///   - sequence variant (single AA variation)
    ///   - active site
    ///   - binding site
    ///   - metal binding
    ///   - site
    /// </summary>
    public class Annotation
    {
        public List<UniProtEntryDetails> EntryAnnotations { get; set; }
        public ResidueLevelAnnotations ResidueAnnotations { get; set; }

        public Annotation()
        {
            EntryAnnotations = new List<UniProtEntryDetails>();
            ResidueAnnotations = new ResidueLevelAnnotations() { ChannelsDB = new List<ResidueAnnotation>(), UniProt = new List<ResidueAnnotation>() };
        }

        public void AddResidues((string, Dictionary<string, string>) uniProtMapping, XElement root)
        {
            Dictionary<string, (string, string)> refs = ParseReferences(root.Elements(NS("evidence")).ToArray());

            var tempVar = ExtractElements(root, "sequence variant");
            var tempAS = ExtractElements(root, "active site");
            var tempBS = ExtractElements(root, "binding site");
            var tempMutS = ExtractElements(root, "mutagenesis site");
            var tempS = ExtractElements(root, "site");
            var tempMetBs = ExtractElements(root, "metal ion-binding site");

            AddCustomSite(uniProtMapping, tempS, refs);
            AddCustomSite(uniProtMapping, tempAS, refs);
            AddCustomSite(uniProtMapping, tempBS, refs);
            AddCustomSite(uniProtMapping, tempMetBs, refs);
            AddVariants(uniProtMapping, tempMutS, refs);
            AddVariants(uniProtMapping, tempVar, refs);
        }


        /// <summary>
        /// Add sequence variants annotations
        /// </summary>
        /// <param name="uniProtMapping"></param>
        /// <param name="tempVar"></param>
        /// <param name="refs"></param>
        private void AddVariants((string, Dictionary<string, string>) uniProtMapping, XElement[] tempVar, Dictionary<string, (string, string)> refs)
        {
            foreach (var item in tempVar)
            {
                try
                {
                    var original = item?.Element(NS("original"));

                    if (original != null)
                    {
                        var temp = new ResidueAnnotation()
                        {
                            Chain = uniProtMapping.Item1,
                            Id = uniProtMapping.Item2[item.Element(NS("location")).Element(NS("position")).Attribute("position").Value],
                            Reference = refs[item.Attribute("evidence").Value.Split(new char[0])[0]].Item1,
                            ReferenceType = refs[item.Attribute("evidence").Value.Split(new char[0])[0]].Item2,
                            Text = BuildReferenceText(item),
                        };

                        if(temp.Id != "null") ResidueAnnotations.UniProt.Add(temp);
                    }
                }
                catch (Exception) { }
            }
        }       


        /// <summary>
        /// Parse references for the UniProt annotations
        /// </summary>
        /// <param name="references"></param>
        /// <returns></returns>
        private Dictionary<string, (string, string)> ParseReferences(XElement[] references)
        {
            var result = new Dictionary<string, (string, string)>();
            foreach (var item in references)
            {
                try
                {
                    var key = item.Attribute("key").Value;
                    var dbRef = item.Element(NS("source")).Element(NS("dbReference"));

                    result.Add(key, (dbRef.Attribute("id").Value, dbRef.Attribute("type").Value));
                }
                catch (Exception) { }
            }
            return result;

        }



        private void AddCustomSite((string, Dictionary<string, string>) uniProtMapping, XElement[] sites, Dictionary<string, (string, string)> refs)
        {
            foreach (var item in sites)
            {
                try
                {
                    var evidenceAttr = item.Attribute("evidence")?.Value;

                    var temp = new ResidueAnnotation()
                    {
                        Text = $"{ToTitleCase(item.Attribute("type").Value)}: {item.Attribute("description").Value}.",
                        Reference = evidenceAttr == null? string.Empty : refs[evidenceAttr].Item1,
                        ReferenceType = evidenceAttr == null ? string.Empty : refs[evidenceAttr].Item2,
                        Chain = uniProtMapping.Item1,
                        Id = uniProtMapping.Item2[item.Element(NS("location")).Element(NS("position")).Attribute("position").Value]
                    };

                    if (temp.Id != "null") ResidueAnnotations.UniProt.Add(temp);
                }
                catch (Exception) { }
            }
        }




        public void AddChannelResidues((string, Dictionary<string, string>) uniprotMapping, string path)
        {
            if (!File.Exists(path)) return;

            var ann = JsonConvert.DeserializeObject<ResidueAnnotation[]>(File.ReadAllText(path));

            foreach (var item in ann)
            {
                item.Chain = uniprotMapping.Item1;
                item.Id = uniprotMapping.Item2.ContainsKey(item.Id) ? uniprotMapping.Item2[item.Id] : "null";
            }

            ResidueAnnotations.ChannelsDB.AddRange(ann);
        }


        private string BuildReferenceText(XElement item)
        {
            var orig = item.Element(NS("original")).Value;
            var var = item.Element(NS("variation")).Value;
            var desc = item.Attribute("description").Value;

            return $"{orig} &rarr; {var}, {desc}";

        }


        private XElement[] ExtractElements(XElement root, string attribute) => root.Elements(NS("feature")).Where(x => String.Compare(x.Attribute("type").Value, attribute, true) == 0).ToArray();

        /// <summary>
        /// Add name to the http://uniprot.org/uniprot namespace
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private string NS(string element) => $"{{http://uniprot.org/uniprot}}{element}";



        private static string ToTitleCase(string str)
        {
            var tokens = str.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                tokens[i] = token.Substring(0, 1).ToUpper() + token.Substring(1).ToLower();
            }

            return string.Join(" ", tokens);
        }
    }
}
