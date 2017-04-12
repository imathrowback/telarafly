using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Text;
using System.IO;
using UnityEngine;

namespace Assets.NIF
{

    public class NIFObject
    {
        public String typeName;
        public int nifSize;
        public String extraDataString = "";
        public int parentIndex = -1;
        public int index;
        public String name;
        public List<int> extraDataIDs;
        public List<NifStreamElement> streamElems;

        public virtual void parse(NIFFile file, NIFObject baseo, BinaryReader dis)
        {
            nifSize = baseo.nifSize;
            typeName = baseo.typeName;
            index = baseo.index;
        }

        protected int loadLinkID(BinaryReader ds)
        {
            return ds.readInt();
        }



        protected void loadExtraData(NIFFile file, BinaryReader ds)
        {
            loadObject(file, ds);
            extraDataString = file.loadString(ds);
        }

        private void loadObject(NIFFile file, BinaryReader ds)
        {
            // do nothing, unless the file version is < 10.1.0.114 which we don't care about right now
        }

        protected List<int> loadLinkIDs(BinaryReader ds)
        {
            uint NIF_INVALID_LINK_ID_COUNT = 0xFFFFFFFF;
            uint NIF_INVALID_STRING_INDEX = 0xFFFFFFFF;
            int NIF_MAX_SANE_LINK_ID_COUNT = 8192;
            int numLinkIDs = ds.readInt();
            if (numLinkIDs == NIF_INVALID_LINK_ID_COUNT)
                return new List<int>();
            if (numLinkIDs > NIF_MAX_SANE_LINK_ID_COUNT)
                throw new Exception("Suspicious count");

            List<int> ids = new List<int>();
            for (int i = 0; i < numLinkIDs; i++)
                ids.Add(ds.readInt());

            return ids;
        }

         List<NIFObject> children = new List<NIFObject>();
        internal void addChild(NIFObject nIFObject)
        {
            children.Add(nIFObject);
        }
    }
}
