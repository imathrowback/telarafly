using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.NIF
{
    public class NiNode : NiAVObject
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
