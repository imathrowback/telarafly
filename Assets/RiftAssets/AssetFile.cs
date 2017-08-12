using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.RiftAssets
{
    public class AssetFile
    {
        public string file;
        public bool is64 = false;
        Dictionary<string, AssetEntry> assets = new Dictionary<string, AssetEntry>();


        public AssetFile( string file) 
        {
		    this.file = file;
            is64 = (file.Contains("64"));
        }

        public List<AssetEntry> getEntries()
        {
            return assets.Values.ToList();
        }

        public void addAsset(AssetEntry assetEntry)
        {
            if (assets.ContainsKey(assetEntry.strID))
                throw new Exception(
                        "Asset key [" + assetEntry + "] already exists in db[" + assets[assetEntry.strID] + "]");
            assets[assetEntry.strID] = assetEntry;
        }

        public bool contains( string id)
        {
            return assets.ContainsKey(id);
        }

        public bool contains(byte[] id)
        {
            string strID = Util.bytesToHexString(id);
            return contains(strID);
        }

        public AssetEntry getEntry( string strID)
        {
            return assets[strID];
        }

        public AssetEntry getEntry( byte[] id)
        {
            return getEntry(Util.bytesToHexString(id));
        }

        static AssetCache cache = AssetCache.inst;
        /**
         * Attempt to extract the given assetentry into a byte array. Because the content may be compressed the returned byte array
         * may be larger than the requested max bytes.
         *
         * @param entry The entry to read
         * @param maxBytesToRead The maximum bytes to read from the source
         * @return The bytes read, may be larger than requested if the data is compressed
         */
        public byte[] extractPart(AssetEntry entry, int maxBytesToRead, Stream os,
                 bool nodecomp)
        {

            if (entry.file != this)
                throw new Exception(
                        "Extract called on wrong asset file[" + file + "] for asset:" + entry);

            byte[] data = cache.GetOrAdd(entry.strID + ":" + maxBytesToRead + ":" + nodecomp, () => extractPart1(entry, maxBytesToRead,  nodecomp));
            if (os != null)
                os.Write(data, 0, data.Length);
            return data;
        }

        byte[] unzipCache = new byte[0];
        System.Object unzipLock = new System.Object();
        private byte[] extractPart1(AssetEntry entry, int maxBytesToRead,  bool nodecomp)
        {

            try
            {
                
                if (nodecomp || !entry.compressed)
                {
                    using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        // if not compressed
                        
                        byte[] data = new byte[maxBytesToRead];
                        stream.Seek(entry.offset, SeekOrigin.Begin);
                        long bytesRead = stream.Read(data, 0, maxBytesToRead);
                        if (entry.size >= maxBytesToRead && bytesRead != maxBytesToRead)
                            throw new Exception("Not enough bytes read, expected [" + maxBytesToRead + "], got: " + bytesRead);
                      
                        return data;
                    }
                }
                else
                {
                    // COMPRESSED

                    // NOTE: entry.size doesn't indicate the size of the uncompressed data

                    // Check if we want to read all the data or only a little
                    bool readAll = maxBytesToRead >= entry.size;

                    using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        long pos = stream.Seek(entry.offset, SeekOrigin.Begin);

                        lock (unzipLock)
                        {
                           // Debug.Log("extract asset:" + entry);
                            //Debug.Log("decompress asset:" + entry.strID + ", size:" + entry.size + ", sizeD:" + entry.sizeD);
                            if (unzipCache.Length < entry.sizeD)
                            {
                                //Debug.Log("Increasing unzip cache size from " + unzipCache.Length + " to " + entry.sizeD);
                                unzipCache = new byte[entry.sizeD];
                            }
                            int writeIndex = 0;
                            using (ZlibStream ds = new ZlibStream(stream, Ionic.Zlib.CompressionMode.Decompress))
                            {
                                int numRead;
                                while ((numRead = ds.Read(unzipCache, writeIndex, unzipCache.Length - writeIndex)) > 0)
                                {
                                    //Debug.Log("read " + numRead + " into array at " + writeIndex + ", we tried to read " + (unzipCache.Length - writeIndex) + " bytes");
                                    writeIndex += numRead;
                                    if (!readAll && writeIndex >= maxBytesToRead)
                                        break;
                                }
                                //if (writeIndex != entry.sizeD)
                                   // Debug.LogWarning("expected to read " + entry.sizeD + " bytes, but only got " + writeIndex);
                                /*
                                //Copy the decompression stream into the output file.
                                byte[] buffer = new byte[4096];
                                while ((numRead = ds.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    totalRead += numRead;
                                    if (!readAll && totalRead >= maxBytesToRead)
                                        break;
                                    decompressed.Write(buffer, 0, numRead);

                                }
                                */
                                byte[] outData = new byte[writeIndex];
                                Array.Copy(unzipCache, outData, outData.Length);
                                return outData;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("failure in file " + file + ", @ " + entry.offset + ", id:"
                        + entry.strID + ", compressed?" + entry.compressed + ", filesize:" + entry.size + "\n\t",
                        ex);
            }
        }

        /** Attempt to extract the given assetentry into a byte array */
        public byte[] extract( AssetEntry entry)
        {
            return extractPart(entry, entry.size, null, false);
        }

        public byte[] extract( byte[] id)
        {
            String strID = Util.bytesToHexString(id);
            return extract(assets[strID]);

        }

        public void extract( AssetEntry entry,  Stream fos)
        {
            extractPart(entry, entry.size, fos, false);
        }

        public byte[] extractNoDecomp( AssetEntry entry)
        {
            return extractPart(entry, entry.size, null, true);
        }
    }
}
