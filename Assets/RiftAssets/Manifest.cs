using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Assets.RiftAssets
{
    public class Manifest
    {
        Dictionary<String, String> fileNameHashIDMap;
        Dictionary<String, String> idToNameNameHashMap;
        bool is64;

        public Manifest(String assetsManifest)
        {
            is64 = (assetsManifest.Contains("64"));
            byte[] manifestData = File.ReadAllBytes(assetsManifest);
            processTable(manifestData);
        }

        public Manifest(byte[] manifestData, bool _64)
        {
            this.is64 = _64;
            processTable(manifestData);
        }

        public List<String> getIDs()
        {
            return idToNameNameHashMap.Keys.ToList();
        }

        public List<String> getFileNameHashes()
        {
            return fileNameHashIDMap.Keys.ToList();
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

        /**
         * Check if the given filenam name hash exists in the manifest.
         *
         * Note that just because a hash exists in the manifest doesn't mean it actually has a corresponding asset file.
         *
         * @param filenameHash The hash to check
         * @return True if the hash exists in the manifest, else false
         */
        public bool containsHash(String filenameHash)
        {
            return fileNameHashIDMap.ContainsKey(filenameHash);
        }

        public bool containsHash(byte[] filenameHash)
        {
            return containsHash(Util.bytesToHexString(filenameHash));
        }

        public String getFilenameHashForID(String id)
        {
            return idToNameNameHashMap[id];
        }

        /**
         * Get the ID string for the given filename if it exists.
         *
         * @param filenameHash The hash to check
         * @return The ID if we know it, else throw an exception
         * @throws IllegalArgumentException If the filename was not found
         */
        public String filenameHashToID(String filenameHash)
        {
            if (fileNameHashIDMap.ContainsKey(filenameHash))
                return fileNameHashIDMap[filenameHash];
            else
                throw new Exception("filename hash [" + filenameHash + "] not found in manifest");
        }

        /**
         * Get the ID bytes for the given filename if it exists.
         *
         * @param filenameHash The hash to check
         * @return The ID if we know it, else throw an exception
         * @throws IllegalArgumentException If the filename was not found
         */
        public byte[] findID(String filenameHash)
        {
            String id = fileNameHashIDMap[filenameHash];
            if (id != null)
                return Util.hexStringToBytes(id);
            else
                throw new Exception("filename hash [" + filenameHash + "] not found in manifest");

        }

        public bool getIs64()
        {
            return is64;
        }



        class TableEntry
        {
            public int offset;
            public int tableSize;
            public int count;
            public int stride;
            public string name;

            public TableEntry(string str, BinaryReader dis)
            {
                name = str;
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


        public List<PAKFileEntry> getPAKs()
        {
            return pakFiles;
        }

        public PAKFileEntry getPAK( int index)
        {
            return pakFiles[index];
        }

        public string getPAKName(int index)
        {
            return pakFiles[index].name;
        }

        public List<PAKFileEntry> pakFiles = new List<PAKFileEntry>();
        public List<ManifestEntry> manifestEntries = new List<ManifestEntry>();

        private void processTable(byte[] manifestData)
        {
            readHeader(manifestData);
            readAssetEntries(manifestData);
        }

        private void readAssetEntries(byte[] manifestData)
        { 
            int count = assetNames.count;
            int tableOffset = assetNames.offset;
            int entrySize = 56;
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
    }

        TableEntry assetNames;
        private void readHeader(byte[] manifestData)
        {
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


                    int _256tableoffset = dis.readInt();
                    int _256 = dis.readInt();

                    TableEntry a = new TableEntry("pak table", dis);
                    TableEntry b = new TableEntry("asset names", dis);
                    this.assetNames = b;
                    TableEntry c = new TableEntry("unk", dis);


                    int[] table = new int[256];
                    for (int i = 0; i < _256; i++)
                    {
                        using (BinaryReader dis2 = new BinaryReader(
                                new MemoryStream(manifestData, _256tableoffset + i, 1)))
                        {
                            table[i] = dis2.readByte();
                        }
                    }
                    for (int i = 0; i < a.count; i++)
                    {
                        using (BinaryReader dis2 = new BinaryReader(
                                new MemoryStream(manifestData, a.offset + (i * a.stride), a.stride)))
                        {
                            pakFiles.Add(new PAKFileEntry(manifestData, dis2));

                        }
                    }
                    int stringOffset = a.offset + (a.count * a.stride);
                    using (BinaryReader dis2 = new BinaryReader(
                    new MemoryStream(manifestData, c.offset, c.tableSize)))
                    {
                        for (int i = 0; i < c.count; i++)
                        {
                            int ia = dis2.readInt() & 0xFFFFFF;
                            int ecount = dis2.readInt();
                            int offset = dis2.readInt();
                            //System.out.println(MessageFormat.format("{0}:count:{1}:offset:{2}", ia, ecount, offset));
                            using (BinaryReader dis3 = new BinaryReader(
                                    new MemoryStream(manifestData, offset, ecount * 4)))
                            {
                                for (int j = 0; j < ecount; j++)
                                {
                                    //if (j < 100)
                                    //	System.out.println("[" + j + "]" + dis3.readInt());
                                }
                            }
                        }

                    }
                }
            }
        }
    }
}
