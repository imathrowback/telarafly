using Ionic.Zlib;
using SevenZip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Assets.RiftAssets
{
    public enum RemoteType
    {
        LIVE, PTS
    }

    public class RemotePAK
    {
        Dictionary<string, string> entryMap = new Dictionary<string, string>();

        public delegate void ProgressD(string s);
        public ProgressD progressUpdate = delegate { };

        // Only useful before .NET 4
        public static void CopyToWithProgress(Stream input, long inSize, Stream output, Action<String> progress)
        {
            byte[] buffer = new byte[1024 * 1024]; // Fairly arbitrary size
            int bytesRead;
            long total = 0;
            string pstr = "";

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                total += bytesRead;
                float progressX = ((float)total / (float)inSize) * 100.0f;
                string newp = "" + progressX.ToString("0");
                if (!newp.Equals(pstr))
                {
                    progress.Invoke(newp);
                    pstr = newp;
                }
                output.Write(buffer, 0, bytesRead);
            }
        }

        // Only useful before .NET 4
        public static void CopyTo(Stream input, Stream output)
        {
            byte[] buffer = new byte[16 * 1024]; // Fairly arbitrary size
            int bytesRead;

            while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, bytesRead);
            }
        }

        RemoteType type;
        int index = 4;

        private string getBaseURL(int index)
        {
            switch (type)
            {
                case RemoteType.PTS:
                    return "http://update2.triongames.com/ch1-live-streaming-client-patch/content/patchpts0" + index + "/";
                case RemoteType.LIVE:
                    return "http://rift-update.dyn.triongames.com/ch1-live-streaming-client-patch/content/patchlive0" + index + "/";
            }
            return "";
        }

        public RemotePAK(RemoteType type)
        {
            this.type = type;
            initNameDB();
        }

        public byte[] downloadManifest()
        {
            // TODO: Some kind of sha test
            String cacheFile = Path.Combine(cacheDir, "assets64.manifest");
            if (cache)
            {
                if (File.Exists(cacheFile))
                    return File.ReadAllBytes(cacheFile);
            }

            string url = getBaseURL(index) + "recovery64/assets64.manifest";
            MemoryStream ms = new MemoryStream();

            Debug.Log("Attempt to get:" + url);

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            using (WebResponse response = req.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    Debug.Log("download manifest");

                    CopyToWithProgress(stream, response.ContentLength, ms, (s) => progressUpdate.Invoke("Manifest:" + s + "%"));
                }
            }
            


            ms.Seek(0, SeekOrigin.Begin);
            byte[] data =  ms.ToArray();
            if (cache)
                File.WriteAllBytes(cacheFile, data);
            return data;
        }

        private void initNameDB()
        {
            try
            {
                byte[] data = File.ReadAllBytes(@"decomp\single-entries.dat");
                GZipStream str = new GZipStream(new MemoryStream(data), Ionic.Zlib.CompressionMode.Decompress);
                StreamReader reader = new StreamReader(str);
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    string[] parts = line.Split(':');
                    if (parts.Length == 2)
                        entryMap[parts[0]] = parts[1];
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Unable to load entries database:" + ex.Message);
            }
        }

        bool cache = true;
        String cacheDir = @"c:\temp\cache";
        public byte[] download(Manifest manifest, ManifestEntry e)
        {
            // combine the name and sha hash to make a unique filename
            String cacheFile = Path.Combine(cacheDir, e.hashStr + e.idStr);
            if (cache)
            {
                if (File.Exists(cacheFile))
                    return File.ReadAllBytes(cacheFile);
            }


            string name = e.hashStr;
            if (entryMap.ContainsKey(name))
                name = entryMap[name];

            Debug.Log("downloading " + name);
            MemoryStream ms = new MemoryStream();

            int pakIndex = e.pakIndex;
            PAKFileEntry pakFile = manifest.getPAK(pakIndex);

            string url = getBaseURL(index) + pakFile.name;
            int startBytes = e.pakOffset;
            int endBytes = (e.pakOffset + e.compressedSize) - 1;

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.AddRange(startBytes, endBytes);
            using (WebResponse response = req.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    CopyToWithProgress(stream, response.ContentLength, ms, (s) => progressUpdate.Invoke("entry[" +name + "]:" + s + "%"));
                }
            }


            ms.Seek(0, SeekOrigin.Begin);
            byte[] returnData = null;
            if (e.size != e.compressedSize)
            {
                Debug.Log("lzma decompress " + name);
                byte[] sourceArray = ms.ToArray();
                int sourceLen = sourceArray.Length;
                byte[] destArray = new byte[e.size];
                int destLen = destArray.Length;

                GCHandle src = GCHandle.Alloc(sourceArray, GCHandleType.Pinned);
                GCHandle dst = GCHandle.Alloc(destArray, GCHandleType.Pinned);


                lzma2decode(src.AddrOfPinnedObject(), sourceLen, dst.AddrOfPinnedObject(), destLen);

                src.Free();
                dst.Free();

                returnData = destArray;
            }
            else
            {
                returnData = ms.ToArray();
            }
            if (cache)
                File.WriteAllBytes(cacheFile, returnData);
            return returnData;
        }


#if UNITY_64
        [DllImport(@"riftlzma2_x64")]
        static extern void lzma2decode(IntPtr src, int srcLen, IntPtr dest, int destLen);
#elif UNITY_32
        [DllImport(@"riftlzma2")]
        static extern void lzma2decode(IntPtr src, int srcLen, IntPtr dest, int destLen);
#endif

    }



}
