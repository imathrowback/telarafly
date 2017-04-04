using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    class NiObjectNET : NIFObject
    {
        protected virtual void loadObjectNET(NIFFile file, BinaryReader ds) 
        {
            name = file.loadString(ds);
            extraDataIDs = loadLinkIDs(ds);
            loadLinkIDs(ds);
        }
    }
}
