using ChannelsDB.Core.Models.Annotations;
using System.Collections.Generic;

namespace ChannelsDB.Core.Models
{
    public class ResidueLevelAnnotations
    {
        public List<ResidueAnnotation> ChannelsDB { get; set; }
        public List<ResidueAnnotation> UniProt { get; set; }
    }
}
