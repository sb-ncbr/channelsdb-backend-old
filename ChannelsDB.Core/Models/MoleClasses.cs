using ChannelsDB.Core.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChannelsDB.Core.Models
{

    public class MoleReport
    {
        public string Version { get; set; }
        public Config Config { get; set; }
        public bool FoundOrigin { get; set; }
        public int Timing { get; set; }
        public Channels Channels { get; set; }
        public Volumes Cavities { get; set; }
        public Origins Origins { get; set; }

    }

    public class Config
    {
        public bool InMembrane { get; set; }
        public bool IsBetaBarel { get; set; }
        public object UserStructure { get; set; }
        public string PdbId { get; set; }
        public string WorkingDirectory { get; set; }
        public object[] Chains { get; set; }
        public string PyMolLocation { get; set; }
        public string MemEmbedLocation { get; set; }
        public int Threads { get; set; }
    }

    public class Channels
    {
        public Tunnel[] Tunnels { get; set; }
        public Tunnel[] MergedPores { get; set; }
        public Tunnel[] Pores { get; set; }
        public Tunnel[] Paths { get; set; }

        public string ReportToDownload(string type, string id)
        {
            var tunnels = new List<Tunnel>();

            if (Tunnels != null) tunnels.AddRange(Tunnels);
            if (MergedPores != null) tunnels.AddRange(MergedPores);
            if (Pores != null) tunnels.AddRange(Pores);
            if (Paths != null) tunnels.AddRange(Paths);


            StringBuilder sb = new StringBuilder();
            switch (type)
            {
                case "vmd":
                    VmdExporter vmd = new VmdExporter(sb);
                    vmd.AddFetch(id);
                    vmd.AddTunnels(tunnels);
                    break;
                case "chimera":
                    ChimeraExporter chimera = new ChimeraExporter(sb);
                    chimera.AddTunnels(tunnels);
                    chimera.AddFetch(id);
                    break;

                default:
                    PyMolExporter pymol = new PyMolExporter(sb);
                    pymol.AddTunnels(tunnels);
                    pymol.AddFetch(id);
                    break;
            }

            return sb.ToString();
        }

        public List<Tunnel> ToTunnelsArray()
        {
            var result = new List<Tunnel>();

            result.AddRange(Tunnels);
            result.AddRange(MergedPores);
            result.AddRange(Pores);
            result.AddRange(Paths);

            return result;
        }
    }

    public class Tunnel
    {
        public string Type { get; set; }
        public string Id { get; set; }
        public string Cavity { get; set; }
        public bool Auto { get; set; }
        public Properties Properties { get; set; }
        public Profile[] Profile { get; set; }
        public Layers Layers { get; set; }

    }

    public class Properties
    {
        public int Charge { get; set; }
        public int NumPositives { get; set; }
        public int NumNegatives { get; set; }
        public float Hydrophobicity { get; set; }
        public float Hydropathy { get; set; }
        public float Polarity { get; set; }
        public int Mutability { get; set; }
    }

    public class Layers
    {
        public string[] ResidueFlow { get; set; }
        public string[] HetResidues { get; set; }
        public Layerweightedproperties LayerWeightedProperties { get; set; }
        public Layersinfo[] LayersInfo { get; set; }
    }

    public class Layerweightedproperties
    {
        public float Hydrophobicity { get; set; }
        public float Hydropathy { get; set; }
        public float Polarity { get; set; }
        public int Mutability { get; set; }
    }

    public class Layersinfo
    {
        public Layergeometry LayerGeometry { get; set; }
        public string[] Residues { get; set; }
        public string[] FlowIndices { get; set; }
        public Properties Properties { get; set; }
    }

    public class Layergeometry
    {
        public float MinRadius { get; set; }
        public float MinFreeRadius { get; set; }
        public float StartDistance { get; set; }
        public float EndDistance { get; set; }
        public bool LocalMinimum { get; set; }
        public bool Bottleneck { get; set; }
    }

    public class Profile
    {
        public float Radius { get; set; }
        public float FreeRadius { get; set; }
        public float T { get; set; }
        public float Distance { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Charge { get; set; }
    }

    public class Volumes
    {
        public Cavity[] Cavities { get; set; }
        public Cavity[] Voids { get; set; }
        public Surface Surface { get; set; }
    }

    public class Surface
    {
        public string Type { get; set; }
        public float Volume { get; set; }
        public int Depth { get; set; }
        public double DepthLength { get; set; }
        public int Id { get; set; }
        public Boundary Boundary { get; set; }
        public Inner Inner { get; set; }
        public Properties Properties { get; set; }
        public Mesh Mesh { get; set; }
    }

    public class Boundary
    {
        public string[] Residues { get; set; }
        public Properties Properties { get; set; }
    }

    public class Inner
    {
        public string[] Residues { get; set; }
        public Properties Properties { get; set; }
    }

    public class Mesh
    {
        public float[] Vertices { get; set; }
        public int[] Triangles { get; set; }
    }

    public class Cavity
    {
        public string Type { get; set; }
        public float Volume { get; set; }
        public int Depth { get; set; }
        public float DepthLength { get; set; }
        public int Id { get; set; }
        public Boundary Boundary { get; set; }
        public Inner Inner { get; set; }
        public Properties Properties { get; set; }
        public Mesh Mesh { get; set; }
    }

    public class Origins
    {
        public User User { get; set; }
        public Computed Computed { get; set; }
        public Database Database { get; set; }
        public Inputorigins InputOrigins { get; set; }
        public Inputexits InputExits { get; set; }
        public Inputfoundexits InputFoundExits { get; set; }
    }

    public class User
    {
        public string Type { get; set; }
        public object[] Points { get; set; }
    }

    public class Computed
    {
        public string Type { get; set; }
        public Point[] Points { get; set; }
    }

    public class Point
    {
        public string Id { get; set; }
        public int CavityId { get; set; }
        public int Depth { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public class Database
    {
        public string Type { get; set; }
        public object[] Points { get; set; }
    }

    public class Inputorigins
    {
    }

    public class Inputexits
    {
    }

    public class Inputfoundexits
    {
    }

    public class Residue
    {
        public bool Backbone { get; set; }
        public bool SideChain { get; set; }
        public string Chain { get; set; }
        public string Name { get; set; }
        public int SequenceNumber { get; set; }
        public string InsertionCode { get; set; }

        public Residue(string s)
        {
            var spl = s.Split(new string[0], StringSplitOptions.RemoveEmptyEntries);
            Name = spl[0];
            SequenceNumber = int.Parse(spl[1]);
            Chain = spl[2];
            Backbone = spl.Length == 4;
            SideChain = spl.Length == 3;
            InsertionCode = " ";
        }

        public void ChangeState(string s)
        {
            if (s.Contains("Backbone")) Backbone = true;
            else SideChain = false;
        }

        public override bool Equals(object o)
        {
            if (o == null || GetType() != o.GetType())
                return false;

            Residue r = (Residue)o;
            return (r.Name == Name) && (r.SequenceNumber == SequenceNumber) && (r.Chain == Chain);
        }

        public override int GetHashCode()
        {
            return this.SequenceNumber * this.Name.GetHashCode() * this.Chain.GetHashCode();
        }
    }

}
