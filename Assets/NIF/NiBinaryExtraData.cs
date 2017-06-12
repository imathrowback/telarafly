using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.NIF
{
    public class NiBinaryExtraData : NIFObject
    {

        public int binaryDataSize;
        public byte[] extraData;
        private byte[] decompressedData;
        private static System.Threading.Thread loadThread;


        public byte[] getData()
        {
            if (getDecompressed() != null)
                return decompressedData;
            return extraData;
        }
        public NiBinaryExtraData()
        {

        }

        public byte[] getDecompressed()
        {
            loadThread.Join();
            return decompressedData;
        }

        public override void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            base.parse(file, baseo, ds);

            loadExtraData(file, ds);
            binaryDataSize = ds.readInt();
            if (binaryDataSize > 0)
            {
                extraData = ds.ReadBytes(binaryDataSize);

                loadThread = new System.Threading.Thread(new System.Threading.ThreadStart(tryDecompress));
                loadThread.Start();

            }
        }

        /** return true if the data was compressed */
        public bool wasCompressed()
        {
            return getDecompressed() != null;
        }

        /** Try to the decompress the data if possible, otherwise fail silently */
        private void tryDecompress()
        {
            try
            {
                decompressedData = Ionic.Zlib.ZlibStream.UncompressBuffer(extraData);
            }catch (Exception ex)
            {
                //Debug.Log("data not compressed for obj:" + name + ":" + ex.Message);
            }
        }
    }

}
