using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.DatParser; // Assuming this namespace contains the BinaryReader extensions
using UnityEngine; // Assuming this is used for Debug.Log

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
            // Assuming dis.readInt() is an extension method for BinaryReader
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
        bool is64;

        public List<ManifestPAKFileEntry> pakFiles = new List<ManifestPAKFileEntry>();
        public List<ManifestEntry> manifestEntries = new List<ManifestEntry>();

        // Reinstating the cache dictionaries
        // Using int[] to store indices into manifestEntries
        private Dictionary<string, int[]> filenameEntryIndexDict = new Dictionary<string, int[]>();
        private Dictionary<string, int[]> idEntryIndexDict = new Dictionary<string, int[]>();

        public Manifest(String assetsManifest)
        {
            Debug.Log("Reading manifest:" + assetsManifest);
            is64 = (assetsManifest.Contains("64"));
            byte[] manifestData = File.ReadAllBytes(assetsManifest);
            processTable(manifestData);
        }

        public Manifest(byte[] manifestData, bool _is64)
        {
            this.is64 = _is64;
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

                    // Assuming dis.readInt() is an extension method for BinaryReader
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
                    // Assuming dis2.readByte() is an extension method for BinaryReader
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
                    // Assuming dis2.readInt() is an extension method for BinaryReader
                    int ia = dis2.readInt() & 0xFFFFFF;
                    int ecount = dis2.readInt();
                    int offset = dis2.readInt();
                    using (BinaryReader dis3 = new BinaryReader(
                                new MemoryStream(manifestData, offset, ecount * 4)))
                    {
                        for (int j = 0; j < ecount; j++)
                        {
                            // Reading but not using the data here.
                            // Assuming dis3.readInt() is an extension method for BinaryReader
                            dis3.readInt();
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
                    // Assuming ManifestEntry is a defined class in your project
                    // and its constructor takes a BinaryReader
                    ManifestEntry entry = new ManifestEntry(dis);
                    manifestEntries.Add(entry);
                }
            }

            // --- BETTER CACHE POPULATION LOGIC ---
            // Debug.Log("Building caches...");
            // Use temporary List-based dictionaries for building, then convert to Array-based final dictionaries
            Dictionary<string, List<int>> tempFilenameEntryIndexDict = new Dictionary<string, List<int>>();
            Dictionary<string, List<int>> tempIdEntryIndexDict = new Dictionary<string, List<int>>();

            for (int i = 0; i < manifestEntries.Count; i++)
            {
                ManifestEntry entry = manifestEntries[i];

                // Populate filename hash cache
                if (!tempFilenameEntryIndexDict.TryGetValue(entry.filenameHashStr, out List<int> filenameIndices))
                {
                    filenameIndices = new List<int>();
                    tempFilenameEntryIndexDict.Add(entry.filenameHashStr, filenameIndices);
                }
                filenameIndices.Add(i);

                // Populate ID cache
                if (!tempIdEntryIndexDict.TryGetValue(entry.idStr, out List<int> idIndices))
                {
                    idIndices = new List<int>();
                    tempIdEntryIndexDict.Add(entry.idStr, idIndices);
                }
                idIndices.Add(i);
            }

            // Transfer from temporary List-based dictionaries to final Array-based dictionaries
            foreach (var kvp in tempFilenameEntryIndexDict)
            {
                filenameEntryIndexDict.Add(kvp.Key, kvp.Value.ToArray());
            }

            foreach (var kvp in tempIdEntryIndexDict)
            {
                idEntryIndexDict.Add(kvp.Key, kvp.Value.ToArray());
            }
            // Debug.Log("Caches built.");
            // -------------------------------------
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

        // Updated getFileSize to use the new getEntriesForID which queries the cache
        internal int getFileSize(string id)
        {
            List<ManifestEntry> e = getEntriesForID(id);

            if (e.Count == 0)
                return -1;
            return e[0].size;
        }

        // --- Cache-enabled lookup methods ---
        private List<ManifestEntry> getEntries(string filenameHash)
        {
            // Query the cache directly
            if (filenameEntryIndexDict.TryGetValue(filenameHash, out int[] indices))
            {
                return indices.Select(i => manifestEntries[i]).ToList();
            }
            // If not found in cache, return an empty list (DO NOT ADD TO CACHE HERE)
            return new List<ManifestEntry>();
        }

        // Updated getEntriesForID to use the cache
        private List<ManifestEntry> getEntriesForID(string id)
        {
            // Query the cache directly
            if (idEntryIndexDict.TryGetValue(id, out int[] indices))
            {
                return indices.Select(i => manifestEntries[i]).ToList();
            }
            // If not found in cache, return an empty list (DO NOT ADD TO CACHE HERE)
            return new List<ManifestEntry>();
        }

        /**
         * Check if the given filename name hash exists in the manifest.
         *
         * Note that just because a hash exists in the manifest doesn't mean it actually has a corresponding asset file.
         *
         * @param filenameHash The hash to check
         * @return True if the hash exists in the manifest, else false
         */
        public bool containsHash(string filenameHash)
        {
            // This implicitly uses the cache via getEntries
            return getEntries(filenameHash).Any();
        }

        // This method keeps your custom logic for selecting from multiple IDs
        public ManifestEntry getEntry(string id)
        {
            List<ManifestEntry> entries = getEntriesForID(id);
            if (entries.Count == 0)
                throw new Exception("ID not found in manifest: '" + id + "'");

            // If there are more than one entry, we should iterate through them and return the first one that doesn't have a 0 pakOffset
            if (entries.Count > 1)
            {
                foreach (var entry in entries)
                {
                    if (entry.pakOffset != 0)
                    {
                        return entry; // Return the first entry with a non-zero pakOffset
                    }
                }
            }
            // Otherwise, return the first entry
            return entries[0];
        }

        /**
         * Get the ID string for the given filename if it exists.
         *
         * @param filenameHash The hash to check
         * @return The ID if we know it, else throw an exception
         */
        public IEnumerable<string> filenameHashToID(string filenameHash)
        {
            // This implicitly uses the cache via getEntries
            return getEntries(filenameHash).Select(e => e.idStr);
        }

        public List<ManifestEntry> getEntriesForFilenameHash(string filenameHash)
        {
            // This implicitly uses the cache via getEntries
            return getEntries(filenameHash).ToList();
        }

        public bool getIs64()
        {
            return is64;
        }
    }
}