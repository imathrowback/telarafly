using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    class NiNode : NiAVObject
    {
        public List<int> childLinks;
        public NiNode()
        {

        }

        
        public override void parse( NIFFile file,  NIFObject baseo,  BinaryReader ds) 
        {
            base.parse(file, baseo, ds);

            loadAVObject(file, ds);
            childLinks = loadLinkIDs(ds);
            loadLinkIDs(ds);

        }
    }
}
