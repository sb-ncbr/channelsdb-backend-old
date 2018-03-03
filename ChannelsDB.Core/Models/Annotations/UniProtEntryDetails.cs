using System;
using System.Linq;
using System.Xml.Linq;

namespace ChannelsDB.Core.Models.Annotations
{
    /// <summary>
    /// Contains definition of Function, Name and a list of catalytic activities for UniProt ntry
    /// </summary>
    public class UniProtEntryDetails
    {
        public string Function { get; set; }
        public string Name { get; set; }
        public string[] Catalytics { get; set; }
        public string UniProtId { get; set; }
    
        public UniProtEntryDetails() { }

        public UniProtEntryDetails(string uniProt, XElement element)
        {
            UniProtId = uniProt;
            Function = ProcessFunction(element);
            Name = ProcessName(element);
            Catalytics = ProcessCatalytics(element);



        }

        private string[] ProcessCatalytics(XElement element)
        {
            return element.Elements(NS("comment"))
                ?.Where(x => x.Attribute("type").Value == "catalytic activity")
                .Elements(NS("text"))
                ?.Select(x => x.Value)
                .ToArray();
        }



        private string ProcessName(XElement element)
        {
            var recommended = element.Element(NS("protein"))?.Element(NS("recommendedName"))?.Element(NS("fullName"))?.Value;
            var alternate = element.Element(NS("protein"))?.Element(NS("alternativeName"))?.Element(NS("fullName"))?.Value;
            var submitted = element.Element(NS("protein"))?.Element(NS("submittedName"))?.Element(NS("fullName"))?.Value;

            if (!String.IsNullOrEmpty(recommended)) return recommended;
            if (!String.IsNullOrEmpty(alternate)) return alternate;
            if (!String.IsNullOrEmpty(submitted)) return submitted;

            return string.Empty;
        }

        private string ProcessFunction(XElement element)
        {
            return element.Elements(NS("comment"))?.Where(x => x.Attribute("type").Value == "function").Elements(NS("text")).FirstOrDefault()?.Value;
        }


        private string NS(string element) => $"{{http://uniprot.org/uniprot}}{element}";
    }
}
