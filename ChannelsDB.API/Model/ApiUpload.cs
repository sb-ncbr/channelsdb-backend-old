using ChannelsDB.Core.Models.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChannelsDB.API.Model
{
    public class ApiUpload
    {
        public string PdbId { get; set; }
        public string Email { get; set; }
        public TunnelAnnotations[] TunnelAnnotations { get; set; }
        public ResidueAnnotation[] ResidueAnnotations { get; set; }

    }

    public class MoleTransfer {
        public string CompId { get; set; }
        public int SubmitId { get; set; }
        public string Email { get; set; }
        public string Message { get; set; }

        public TunnelAnnotations[] TunnelAnnotations { get; set; }
        public ResidueAnnotation[] ResidueAnnotations { get; set; }
    }

    public class ApiResponse {
        public string Status { get; set; }
        public string Msg { get; set; }
    }
}
