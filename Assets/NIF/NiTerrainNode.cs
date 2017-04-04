using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    /** We don't really know what this is about from some kind of "node" that can have mesh underlings
     */
    class NiTerrainNode : NiNode
    {
        public override void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            base.parse(file, baseo, ds);
        }

    }
}