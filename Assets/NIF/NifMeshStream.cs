using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    class NifMeshStream
    {

        public int streamLinkID;
        bool instanced;
        public List<int> submeshRegionMap;
        public List<Pair<String, int>> elementDescs;

        public void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            streamLinkID = ds.readInt();
            instanced = ds.readUnsignedByte() > 0;

            int numStreamSubmeshRegionMapEntries = ds.readUnsignedShort();
            submeshRegionMap = new List<int>(numStreamSubmeshRegionMapEntries);
            for (int i = 0; i < numStreamSubmeshRegionMapEntries; i++)
                submeshRegionMap.Add(ds.readUnsignedShort());
            int numElementDescs = ds.readInt();
            elementDescs = new List<Pair<String, int>>(numElementDescs);
            for (int i = 0; i < numElementDescs; i++)
            {
                int descNameIndex = ds.readInt();
                String descName = file.getStringFromTable(descNameIndex);
                int descIndex = ds.readInt();
                elementDescs.Add(NIF.Pair<String, int>.of(descName, descIndex));
            }
        }

    }
}
