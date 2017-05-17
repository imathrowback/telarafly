using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    public class NiFloatsExtraData : NIFObject
    {
        public float[] floatData;

        public NiFloatsExtraData()
        {

        }

        public override void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            base.parse(file, baseo, ds);

            loadExtraData(file, ds);

            int floats = ds.readInt();
            floatData = new float[floats];
            for (int i = 0; i < floats; i++)
                floatData[i] = ds.readFloat();
        }
    }
}
