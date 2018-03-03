using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChannelsDB.API
{
    public class Config
    {
        public string LocalPath { get; set; }
        public string Annotations { get; set; }
        public string UserUploads { get; set; }
        public string MoleTransfers { get; set; }
    }
}
