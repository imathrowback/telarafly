using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.IO.Compression;

namespace Assets.NIF
{
    class NiBinaryExtraData : NIFObject
    {

        int binaryDataSize;
        public byte[] extraData;
        public byte[] decompressed;

        public NiBinaryExtraData()
        {

        }

        public override void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            base.parse(file, baseo, ds);

            loadExtraData(file, ds);
            binaryDataSize = ds.readInt();
            if (binaryDataSize > 0)
            {
                extraData = ds.ReadBytes(binaryDataSize);
                tryDecompress();
            }
        }

        /** return true if the data was compressed */
        public bool wasCompressed()
        {
            return decompressed != null;
        }

        /** Try to the decompress the data if possible, otherwise fail silently */
        private void tryDecompress()
        {
            using (MemoryStream str = new MemoryStream())
            {
                using (DeflateStream ds = new DeflateStream(new MemoryStream(extraData), CompressionMode.Decompress))
                {
                    //Copy the decompression stream into the output file.
                    byte[] buffer = new byte[4096];
                    int numRead;
                    while ((numRead = ds.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        str.Write(buffer, 0, numRead);
                    }
                }
                decompressed = str.GetBuffer();
            }
        }
    }

}
