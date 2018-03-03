using ChannelsDB.Core.Models.Annotations;
using ChannelsDB.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChannelsDB.API.Model
{
    public class ApiResult
    {
        public ChannelObject Channels { get; set; }
        public HashSet<TunnelAnnotations> Annotations { get; set; }
    }

    public class ChannelObject
    {
        public List<Tunnel> CofactorTunnels;
        public List<Tunnel> ReviewedChannels { get; set; }
        public List<Tunnel> CSATunnels { get; set; }
        public List<Tunnel> TransmembranePores { get; set; }
    }
}
