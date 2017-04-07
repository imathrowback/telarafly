using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    public class NiFloatExtraData : NIFObject
    {
        public float floatData;

        public NiFloatExtraData()
        {

        }

        public override void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            base.parse(file, baseo, ds);

            loadExtraData(file, ds);
            floatData = ds.readFloat();
        }
    }
}
