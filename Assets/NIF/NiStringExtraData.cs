using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    class NiStringExtraData : NIFObject
    {
        public String stringExtraData;

        
    public override void parse( NIFFile file,  NIFObject baseo,  BinaryReader ds)
        {
            base.parse(file, baseo, ds);

            loadExtraData(file, ds);
            stringExtraData = file.loadString(ds);

        }
    }
}
