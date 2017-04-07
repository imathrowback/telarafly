using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    public class NiColorExtraData : NIFObject
    {
        public float r;
        public float g;
        public float b;
        public float a;

        public NiColorExtraData()
        {

        }

        public override void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            base.parse(file, baseo, ds);

            loadExtraData(file, ds);
            r = ds.ReadSingle();
            g = ds.ReadSingle();
            b = ds.ReadSingle();
            a = ds.ReadSingle();

        }
    }
}
