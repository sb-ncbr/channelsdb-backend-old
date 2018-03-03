using ChannelsDB.API.Utils;
using ChannelsDB.Core.Models;
using ChannelsDB.Core.Utils;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using ChannelsDB.API.Model;
using ChannelsDB.Core.Models.Annotations;

namespace ChannelsDB.API
{
    public class DbManager
    {
        private Config config;


        public DbManager() { }

        public DbManager(Config c)
        {
            this.config = c;
        }

        public bool CheckId(string id)
        {
            if (id.Length == 4) return Directory.Exists(ProteinDirPath(id));

            return false;
        }


        public ApiResult GetProteinData(string id)
        {
            HashSet<TunnelAnnotations> annotations = new HashSet<TunnelAnnotations>();
            var annotationsJson = Extensions.GetZippedFileFromArchive(ProteinArchive(id), DBFiles.Annotation);

            var csaJson = Extensions.GetZippedFileFromArchive(ProteinArchive(id), DBFiles.CSA);
            var authorJson = Extensions.GetZippedFileFromArchive(ProteinArchive(id), DBFiles.Authors);
            var poreJson = Extensions.GetZippedFileFromArchive(ProteinArchive(id), DBFiles.Pores);
            var cofactorJson = Extensions.GetZippedFileFromArchive(ProteinArchive(id), DBFiles.Cofactor);


            var csaChannels = csaJson == "{}" ? new List<Tunnel>() : JsonConvert.DeserializeObject<MoleReport>(csaJson)?.Channels?.ToTunnelsArray();
            var authorChannels = authorJson == "{}" ? new List<Tunnel>() : JsonConvert.DeserializeObject<MoleReport>(authorJson)?.Channels?.ToTunnelsArray();
            var poreChannels = poreJson == "{}" ? new List<Tunnel>() : JsonConvert.DeserializeObject<MoleReport>(poreJson)?.Channels?.ToTunnelsArray();
            var cofactorChannels = cofactorJson == "{}" ? new List<Tunnel>() : JsonConvert.DeserializeObject<MoleReport>(cofactorJson)?.Channels?.ToTunnelsArray();


            var csaAnot = csaChannels.Select(x => new TunnelAnnotations
            {
                Id = x.Id,
                Name = "Tunnel",
                Description = "",
                Reference = string.Empty,
                ReferenceType = string.Empty
            }).ToList();

            var poreAnot = poreChannels.Select(x => new TunnelAnnotations
            {
                Id = x.Id,
                Name = "Pore",
                Description = "",
                Reference = string.Empty,
                ReferenceType = string.Empty
            }).ToList();
            
            var authorAnot = annotationsJson == "{}" ? new List<TunnelAnnotations>() : JsonConvert.DeserializeObject<List<TunnelAnnotations>>(annotationsJson);

            authorAnot.ForEach(x => annotations.Add(x));
            if (csaAnot.Count() > 0) csaAnot.ForEach(x => annotations.Add(x));
            if (poreAnot.Count() > 0) poreAnot.ForEach(x => annotations.Add(x));

            return new ApiResult()
            {
                Channels = new ChannelObject()
                {
                    ReviewedChannels = authorChannels,
                    CSATunnels = csaChannels,
                    TransmembranePores = poreChannels,
                    CofactorTunnels = cofactorChannels
                },
                Annotations = annotations
            };
        }



        internal async Task<byte[]> GetPicture(string pdbID)
        {
            if (File.Exists(Path.Combine(ProteinDirPath(pdbID), $"{pdbID}.png")))
            {
                return File.ReadAllBytes(Path.Combine(ProteinDirPath(pdbID), $"{pdbID}.png"));
            }

            using (HttpClient c = new HttpClient())
            {
                var xml = c.GetStringAsync($"http://www.ebi.ac.uk/pdbe/static/entry/download/{pdbID}-assembly.xml").Result;
                var id = System.Xml.Linq.XDocument.Parse(xml).Root.Elements("assembly").First(x => x.Attribute("prefered").Value.Equals("True")).Attribute("id").Value;
                var result = await c.GetByteArrayAsync($@"https://www.ebi.ac.uk/pdbe/static/entry/{pdbID}_assembly_{id}_chemically_distinct_molecules_front_image-200x200.png");

                return result;
            }
        }



        public byte[] GetDownloadData(string pdbID)
        {
            MergeChannels(pdbID, out List<Tunnel> allChannels, out ApiResult json);

            StringBuilder pymol = WritePyMolTunnels(allChannels);
            StringBuilder chimera = WriteChimeraTunnels(allChannels);
            StringBuilder vmd = WriteVmdTunnels(allChannels);
            StringBuilder pdb = WritePdbTunnels(allChannels);



            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    ZipString($"{pdbID}_report.json", archive, json.ToJson());
                    ZipString($"{pdbID}_pymol.py", archive, pymol.ToString());
                    ZipString($"{pdbID}_chimera.py", archive, chimera.ToString());
                    ZipString($"{pdbID}_vmd.tk", archive, vmd.ToString());
                    ZipString($"{pdbID}_tunnels.pdb", archive, pdb.ToString());
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }        



        public byte[] GetPyMol(string pdbId) {
            MergeChannels(pdbId, out List<Tunnel> allChannels, out ApiResult json);

            StringBuilder pymol = WritePyMolTunnels(allChannels);

            return Encoding.UTF8.GetBytes(pymol.ToString());
        }

        public byte[] GetPDB(string pdbId)
        {
            MergeChannels(pdbId, out List<Tunnel> allChannels, out ApiResult json);

            StringBuilder pdb = WritePdbTunnels(allChannels);

            return Encoding.UTF8.GetBytes(pdb.ToString());
        }


        private static void ZipString(string fileName, ZipArchive archive, string content)
        {
            var demoFile = archive.CreateEntry(fileName);

            using (var entryStream = demoFile.Open())
            using (var streamWriter = new StreamWriter(entryStream))
            {
                streamWriter.Write(content);
            }
        }



        /// <summary>
        /// Returns path to the archive DBFiles.Archive (data.zip) with all the channels and annotations calculated
        /// </summary>
        /// <param name="id">PDB id</param>
        /// <returns>Path to the archive file DBFiles.Archive (data.zip)</returns>
        private string ProteinDirPath(string id) => Path.Combine(config.LocalPath, id.Substring(1, 2), id);


        private string ProteinArchive(string id) => Path.Combine(ProteinDirPath(id), DBFiles.Archive);


        private void MergeChannels(string pdbID, out List<Tunnel> allChannels, out ApiResult json)
        {
            allChannels = new List<Tunnel>();
            json = GetProteinData(pdbID);

            allChannels.AddRange(json.Channels.ReviewedChannels);
            allChannels.AddRange(json.Channels.CSATunnels);
            allChannels.AddRange(json.Channels.TransmembranePores);
            allChannels.AddRange(json.Channels.CofactorTunnels);
        }


        #region export
        private StringBuilder WriteVmdTunnels(IEnumerable<Tunnel> tunnels)
        {
            var sb = new StringBuilder();

            VmdExporter e = new VmdExporter(sb);

            e.AddTunnels(tunnels.ToList());

            return sb;
        }


        private StringBuilder WriteChimeraTunnels(IEnumerable<Tunnel> tunnels)
        {
            var sb = new StringBuilder();

            ChimeraExporter e = new ChimeraExporter(sb);

            e.AddTunnels(tunnels.ToList());

            return sb;
        }


        private StringBuilder WritePyMolTunnels(IEnumerable<Tunnel> tunnels)
        {
            var sb = new StringBuilder();
            PyMolExporter e = new PyMolExporter(sb);
            e.AddTunnels(tunnels.ToList());

            return sb;
        }


        private StringBuilder WritePdbTunnels(IEnumerable<Tunnel> tunnels)
        {
            var sb = new StringBuilder();
            PDBExporter e = new PDBExporter(sb);
            e.AddTunnels(tunnels.ToList());

            return sb;
        }
        #endregion
    }
}
