using Assets.DatParser;
using CGS;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.RiftAssets
{
    public class LangEntry
    {
        public int key;
        public byte[] cdata;
        private string str;

        public string text
        {
            get
            {
                if (str != null)
                    return str;

                CObject obj = DatParser.Parser.processStreamObject(cdata);
                str = "" + obj.get(0).get(1).get(0).convert();
                return str;
            }
        }
    }
    public class EnglishLang
    {

        public Dictionary<int, LangEntry> stringMap = new Dictionary<int, LangEntry>();

        public string get(int i)
        {
            return stringMap[i].text;
        }
        public EnglishLang(AssetDatabase adb, Action<String> progress)
        {
            byte[] englishData = adb.extractUsingFilename("lang_english.cds");
            string expectedChecksum = BitConverter.ToString(System.Security.Cryptography.SHA1.Create().ComputeHash(englishData));

            string actualChecksum = "";
            bool match = false;

            if (File.Exists("cds.xmlz"))
                actualChecksum = BitConverter.ToString(readHash());
            match = actualChecksum.Equals(expectedChecksum);
            Debug.Log("cds.xmlz expected:" + expectedChecksum + " was " + actualChecksum);
            if (!match)
            {
                Debug.Log("no match, recreate");
                stringMap = create(adb.extractUsingFilename("lang_english.cds"), progress);
            }
            else
            {
                Debug.Log("matched, reload");
                stringMap = readData("cds.xmlz", progress);
            }
        }


        internal static Dictionary<int, LangEntry> create(byte[] data, Action<String> progress)
        {
            string temp = Path.GetTempFileName();
            try
            {
                progress.Invoke("Decoding lang database, wait a bit..");
                File.WriteAllBytes(temp, data);
                System.Diagnostics.Process pr;
                pr = new System.Diagnostics.Process();
                pr.StartInfo.FileName = @"decomp\cdsdecomp.exe"; 
                pr.StartInfo.Arguments = "\"" + temp + "\"";
                pr.Start();
                pr.WaitForExit();

                return readData("cds.xmlz", progress);
            }
            finally
            {
                File.Delete(temp);
            }
        }

        private static byte[] readHash()
        {
            using (FileStream fs = new FileStream("cds.xmlz", FileMode.Open))
            {
                using (ProgressStream ps = new ProgressStream(fs))
                {
                    long total = fs.Length;

                    using (DeflateStream ds = new DeflateStream(ps, CompressionMode.Decompress))
                    {
                        long pos = fs.Position;
                        using (BinaryReader reader = new BinaryReader(ds))
                        {
                            byte[] hash = reader.ReadBytes(20);
                            return hash;
                        }
                    }
                }
            }
        }
        private static Dictionary<int, LangEntry> readData(string cdsZ, Action<String> progress)
        {

            Dictionary<int, LangEntry> data = new Dictionary<int, LangEntry>();
            using (FileStream fs = new FileStream("cds.xmlz", FileMode.Open))
            {
                using (ProgressStream ps = new ProgressStream(fs))
                {
                    long total = fs.Length;
                    ps.BytesRead += (s, a) => progress.Invoke("Loading English Database: " + (int)(((float)a.StreamPosition / (float)total) * 100.0) + " %");


                    using (DeflateStream ds = new DeflateStream(ps, CompressionMode.Decompress))
                    {
                        long pos = fs.Position;
                        using (BinaryReader reader = new BinaryReader(ds))
                        {
                            byte[] hash = reader.ReadBytes(20);
                            int entries = reader.ReadInt32();
                            for (int i = 0; i < entries; i++)
                            {
                                int key = reader.ReadInt32();
                                int dataSize = reader.ReadInt32();
                                byte[] cdata = reader.ReadBytes(dataSize);
                                LangEntry lentry = new LangEntry();
                                lentry.cdata = cdata;
                                lentry.key = key;
                                data[key] = lentry;


                            }
                        }
                    }
                }
                return data;
            }
        }
    }
}
