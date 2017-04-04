using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    class NiIntegerExtraData : NIFObject
    {
        public int intExtraData;

        public NiIntegerExtraData()
        {

        }

        public override void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            base.parse(file, baseo, ds);

            loadExtraData(file, ds);
            intExtraData = ds.readInt();

        }
    }
}
