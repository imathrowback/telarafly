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
            try
            {
                decompressed = Ionic.Zlib.ZlibStream.UncompressBuffer(extraData);
            }catch (Exception ex)
            {
                //Debug.Log("data not compressed for obj:" + name + ":" + ex.Message);
            }
        }
    }

}
