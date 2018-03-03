using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using System.Net.Http;
using FluentFTP;

namespace ChannelsDB.Core.Utils
{
    public static partial class Extensions
    {
        public static string DownloadFTPCompressedGz(string host, string url) {
            using (FtpClient c = new FtpClient(host))
            {
                using (Stream s = c.OpenRead(url))
                {
                    using (GZipStream stream = new GZipStream(s, CompressionMode.Decompress))
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
        }



        public static void DownloadFile(string url, string destination) {
            using (HttpClient cl = new HttpClient()) 
            {
                using (Stream s = cl.GetStreamAsync(url).Result)
                {
                    using (Stream file = new FileStream(destination, FileMode.Create))
                    {
                        s.CopyTo(file);
                    }
                }
            }
        }


        public static string DownloadCompressedGz(string url) {

            using (HttpClient c = new HttpClient())
            {
                using (Stream s = c.GetStreamAsync(url).Result)
                {
                    using (GZipStream stream = new GZipStream(s, CompressionMode.Decompress))
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }            
            }
        }


        public static string ToJson(this object o) => JsonConvert.SerializeObject(o, Formatting.Indented);



        /// <summary>
        /// Returns XElement irrespective of its namespace
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="localName"></param>
        /// <returns></returns>
        public static IEnumerable<XElement> ElementsAnyNS<T>(this T source, string localName) where T : XContainer
        {
            return source.Elements().Where(e => e.Name.LocalName == localName);
        }


        /// <summary>
        /// Add a key and value to the dictionary in case the key is unique
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        public static void AddUnique(this Dictionary<string, string> dict, string key, string val)
        {
            if (!dict.ContainsKey(key)) dict.Add(key, val);
        }
    }
}
