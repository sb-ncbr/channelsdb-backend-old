using System;

namespace ChannelsDB.Core.Models.Annotations
{
    public class TunnelAnnotations
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Reference { get; set; }
        public string ReferenceType { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (obj.GetType() != GetType()) return false;

            return ((TunnelAnnotations)obj).Id.Equals(this.Id);
        }

        public override int GetHashCode()
        {
            if (double.TryParse(Id, out double hash)) return Convert.ToInt32(hash);

            return Id.GetHashCode();
        }
    }

    
}
