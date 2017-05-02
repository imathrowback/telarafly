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

        public List<String> getIDs()
        {
            return idToNameNameHashMap.Keys.ToList();
        }

        public List<String> getFileNameHashes()
        {
            return fileNameHashIDMap.Keys.ToList();
        }

        private void processTable(byte[] manifestData)
        {
            // See http://forum.xentax.com/viewtopic.php?f=17&t=10119
            using (MemoryStream memStream = new MemoryStream(manifestData))
            {
                using (BinaryReader dis = new BinaryReader(memStream))
                {

                    int tableOffset;
                    int count;
                    // read the manifest header


                    byte[] twam = dis.ReadBytes(4);
                    // These two are definitely version numbers
                    ushort majorV = dis.ReadUInt16();
                    ushort minorV = dis.ReadUInt16();
                    // skip the next 24 bytes as we don't know what they are
                    dis.ReadBytes(24);
                    tableOffset = dis.ReadInt32();
                    int unknown = dis.ReadInt32(); // unknown
                    count = dis.ReadInt32();


                    // each manifest entry is 56 bytes but we only actually read the first 12, we don't know what the rest are
                    int entrySize = 56;
                    byte[] id = new byte[8];
                    byte[] filenamehash = new byte[4];

                    fileNameHashIDMap = new Dictionary<String, String>(count);
                    idToNameNameHashMap = new Dictionary<String, String>(count);

                    for (int i = 0; i < count; i++)
                    {
                        int start = tableOffset + (i * entrySize);
                        memStream.Seek(start, SeekOrigin.Begin);
                        // read the ID of the entry
                        dis.Read(id, 0, 8);

                        // read the filename hash of the entry
                        dis.Read(filenamehash, 0, 4);
                        reverse(filenamehash);


                        // store the ID and filename hash into a map for easy lookup
                        String idStr = Util.bytesToHexString(id);
                        String hashStr = Util.bytesToHexString(filenamehash);

                        idToNameNameHashMap[idStr] = hashStr;
                        fileNameHashIDMap[hashStr] = idStr;
                    }
                }
            }
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

    }
}
