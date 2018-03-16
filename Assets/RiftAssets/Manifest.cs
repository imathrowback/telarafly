using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.DatParser;
using UnityEngine;

namespace Assets.RiftAssets
{
    class ManifestTableEntry
    {
        public int offset { get; }
        public int tableSize { get; }
        public int count { get; }
        public int stride { get; }
        public string name { get; }

        public ManifestTableEntry(string name, BinaryReader dis)
        {
            this.name = name;
            offset = dis.readInt();
            tableSize = dis.readInt();
            count = dis.readInt();
            stride = dis.readInt();
        }


        override public string ToString()
        {
            int bytes = stride * count;
            int extra = tableSize - bytes;
            return (String.Format(
                "\t[" + name
                        + "]\n\ttableoffset:{0}\n\ttable size in bytes:{1}(extra: {4})\n\tcount:{2}\n\tstride:{3}\n",
                offset, tableSize, count, stride, extra));
        }
    }

    public class Manifest
    {
        //Dictionary<string, string> fileNameHashIDMap;
        //Dictionary<string, string> idToNameNameHashMap;
        bool is64;

        public List<ManifestPAKFileEntry> pakFiles = new List<ManifestPAKFileEntry>();
        public List<ManifestEntry> manifestEntries = new List<ManifestEntry>();

        public Manifest(String assetsManifest)
        {
            Debug.Log("Reading manifest:" + assetsManifest);
            is64 = (assetsManifest.Contains("64"));
            byte[] manifestData = File.ReadAllBytes(assetsManifest);
            processTable(manifestData);
        }

        public IEnumerable<ManifestPAKFileEntry> getPAKs()
        {
            return pakFiles.AsEnumerable();
        }

        public ManifestPAKFileEntry getPAK(int index)
        {
            return pakFiles[index];
        }

        public String getPAKName(int index)
        {
            return getPAK(index).name;
        }

     

        private void processTable(byte[] manifestData)
        {
            //Debug.Log("process manifest table");
            int tableOffset;
            int count;
            ManifestTableEntry a, b, c;
            int _256tableoffset;
            int _256;

            // See http://forum.xentax.com/viewtopic.php?f=17&t=10119
            using (MemoryStream memStream = new MemoryStream(manifestData))
            {
                using (BinaryReader dis = new BinaryReader(memStream))
                {


                    // read the manifest header


                    byte[] twam = dis.ReadBytes(4);
                    // These two are definitely version numbers
                    ushort majorV = dis.ReadUInt16();
                    ushort minorV = dis.ReadUInt16();

                    _256tableoffset = dis.readInt();
                    _256 = dis.readInt();

                    a = new ManifestTableEntry("pak table", dis);
                    b = new ManifestTableEntry("asset names", dis);
                    c = new ManifestTableEntry("unk", dis);

                }
            }
            tableOffset = b.offset;
            count = b.count;

           // Debug.Log("read table 0");
            // why is there a 256 table at the start?
            /** TABLE 0 - 256 byte table */
            byte[] table = new byte[256];
            for (int i = 0; i < _256; i++)
            {
                using (BinaryReader dis2 = new BinaryReader(new MemoryStream(manifestData, _256tableoffset + i, 1)))
                {
                    table[i] = dis2.readByte();
                }
            }

            /** TABLE 1 - PAK files */
           // Debug.Log("read table 1");

            for (int i = 0; i < a.count; i++)
            {
                using (BinaryReader dis2 = new BinaryReader(
                        new MemoryStream(manifestData, a.offset + (i * a.stride), a.stride)))
                {
                    pakFiles.Add(new ManifestPAKFileEntry(manifestData, dis2));

                }
            }

           // Debug.Log("read table 3");

            /** TABLE 3 - unknown? */
            using (BinaryReader dis2 = new BinaryReader(
                    new MemoryStream(manifestData, c.offset, c.tableSize)))
            {
                for (int i = 0; i < c.count; i++)
                {
                    int ia = dis2.readInt() & 0xFFFFFF;
                    int ecount = dis2.readInt();
                    int offset = dis2.readInt();
                    using (BinaryReader dis3 = new BinaryReader(
                            new MemoryStream(manifestData, offset, ecount * 4)))
                    {
                        for (int j = 0; j < ecount; j++)
                        {
                        }
                    }
                }

            }

           // Debug.Log("read table 2");

            /** TABLE 2 - Manifest entries */
            int entrySize = b.stride;
            for (int i = 0; i < count; i++)
            {
                int start = tableOffset + (i * entrySize);

                using (BinaryReader dis = new BinaryReader(
                        new MemoryStream(manifestData, start, entrySize)))
                {
                    ManifestEntry entry = new ManifestEntry(dis);
                    manifestEntries.Add(entry);
                }
            }
           // Debug.Log("build cache");

            for (int i = 0; i < manifestEntries.Count; i++)
            {
                ManifestEntry e = manifestEntries[i];
                putFilenameCacheEntry(e, i);
                putIDCacheEntry(e, i);
            }

        }

        private void putIDCacheEntry(ManifestEntry e, int i)
        {
            List<int> indices;
            idEntryIndexDict.TryGetValue(e.idStr, out indices);
            if (indices == null)
            {
                indices = new List<int>();
                idEntryIndexDict[e.idStr] = indices;
            }

            indices.Add(i);
            
        }

        private void putFilenameCacheEntry(ManifestEntry e, int i )
        {
            List<int> indices;
            filenameEntryIndexDict.TryGetValue(e.filenameHashStr, out indices);
            if (indices == null)
            {
                indices = new List<int>();
                filenameEntryIndexDict[e.filenameHashStr] = indices;
            }

            indices.Add(i);

        }

        internal int getFileSize(string id)
        {
            List<ManifestEntry> e = getEntriesForID(id);
           
            if (e.Count == 0)
                return -1;
            return e[0].size;
        }

        void reverse(byte[] arr)
        {
            for (int i = 0; i < arr.Length / 2; i++)
            {
                byte tmp = arr[i];
                arr[i] = arr[arr.Length - i - 1];
                arr[arr.Length - i - 1] = tmp;
            }
        }

        Dictionary<string, List<int>> filenameEntryIndexDict = new Dictionary<string, List<int>>();
        Dictionary<string, List<int>> idEntryIndexDict = new Dictionary<string, List<int>>();

        private List<ManifestEntry> getEntries(string filenameHash)
        {
            List<int> list = new List<int>();
            filenameEntryIndexDict.TryGetValue(filenameHash, out list);
            if (list == null)
            {
                list = new List<int>();
                filenameEntryIndexDict[filenameHash] = list;
            }

            return list.Select(e => manifestEntries[e]).ToList();
        }

        private List<ManifestEntry> getEntriesForID(string id)
        {
          

            List<int> list = new List<int>();
            idEntryIndexDict.TryGetValue(id, out list);
            if (list == null)
            {
                
                list = new List<int>();
                idEntryIndexDict[id] = list;
            }
            
            return list.Select(e => manifestEntries[e]).ToList();
        }

        /**
         * Check if the given filenam name hash exists in the manifest.
         *
         * Note that just because a hash exists in the manifest doesn't mean it actually has a corresponding asset file.
         *
         * @param filenameHash The hash to check
         * @return True if the hash exists in the manifest, else false
         */
        public bool containsHash(string filenameHash)
        {
           return getEntries(filenameHash).Any();
        }

        /**
         * Get the ID string for the given filename if it exists.
         *
         * @param filenameHash The hash to check
         * @return The ID if we know it, else throw an exception
         */
        public IEnumerable<string> filenameHashToID(string filenameHash)
        {
            return getEntries(filenameHash).Select(e => e.idStr);
        }

        public List<ManifestEntry> getEntriesForFilenameHash(string filenameHash)
        {
            return getEntries(filenameHash).ToList();
        }
        /*
        public List<ManifestEntry> getEntriesForID(string id)
        {
            return manifestEntries.Where(e => e.idStr.Equals(id)).ToList();
        }
        */



        public bool getIs64()
        {
            return is64;
        }

    }
}
