using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;


namespace Assets.RiftAssets
{
    public class AssetFile
    {
        public string file;
        public bool is64 = false;
        Dictionary<String, AssetEntry> assets = new Dictionary<String, AssetEntry>();


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

        public bool contains( String id)
        {
            return assets.ContainsKey(id);
        }

        public bool contains(byte[] id)
        {
            String strID = Util.bytesToHexString(id);
            return contains(strID);
        }

        public AssetEntry getEntry( String strID)
        {
            return assets[strID];
        }

        public AssetEntry getEntry( byte[] id)
        {
            return getEntry(Util.bytesToHexString(id));
        }

        static AssetCache cache = new AssetCache();
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

                        MemoryStream decompressed = new MemoryStream();
                        
                        using (ZlibStream ds = new ZlibStream(stream, Ionic.Zlib.CompressionMode.Decompress))
                        {
                            //Copy the decompression stream into the output file.
                            byte[] buffer = new byte[4096];
                            int numRead;
                            while ((numRead = ds.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                if (!readAll && decompressed.Length >= maxBytesToRead)
                                    break;
                                decompressed.Write(buffer, 0, numRead);
                                
                            }
                        }
                        return decompressed.ToArray();
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
