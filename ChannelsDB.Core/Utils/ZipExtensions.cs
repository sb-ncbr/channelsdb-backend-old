using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace ChannelsDB.Core.Utils
{
    public static partial class Extensions
    {
        /// <summary>
        /// Reads string file from the specified archive
        /// </summary>
        /// <param name="archive">Archive in gzip format</param>
        /// <param name="filename">filename to be found in the gzip</param>
        /// <returns>String representation of channels or error message.</returns>
        public static string GetZippedFileFromArchive(string archive, string filename)
        {
            try
            {
                using (var zip = ZipFile.Open(archive, ZipArchiveMode.Read))
                {
                    var entry = zip.GetEntry(filename);
                    if (entry == null)
                        return "{}";

                    using (var stream = entry.Open())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception)
            {
                return "{}";
            }
        }


        public static void CreateArchiveFromFile(string archiveDest, string fileLocation, string filename)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    var demoFile = archive.CreateEntry(filename);

                    using (var entryStream = demoFile.Open())
                    using (var streamWriter = new StreamWriter(entryStream))
                    {
                        streamWriter.Write(File.ReadAllText(fileLocation));
                    }
                }

                using (var fileStream = new FileStream(archiveDest, FileMode.Create))
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.CopyTo(fileStream);
                }
            }
        }



        public static bool DeleteFileFromArchive(string archive, string filename)
        {
            try
            {
                using (var zip = ZipFile.Open(archive, ZipArchiveMode.Update))
                {
                    zip.Entries.First(x => x.Name == filename).Delete();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        /// <summary>
        /// Add file to a designated zip archive. Creates one if it does not exists
        /// </summary>
        /// <param name="archive">zip file</param>
        /// <param name="sourceFile"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static bool AddFileToArchive(string archive, string sourceFile, string filename)
        {
            if (File.Exists(archive))
            {

                try
                {
                    using (var zip = ZipFile.Open(archive, ZipArchiveMode.Update))
                    {
                        var oldEntry = zip.Entries.FirstOrDefault(x => x.Name == filename);
                        if (oldEntry != null) oldEntry.Delete();

                        zip.CreateEntryFromFile(sourceFile, filename);
                    }
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(archive));
                    CreateArchiveFromFile(archive, sourceFile, filename);
                    return true;
                }
                catch (Exception) { return false; }
            }

        }



    }
}
