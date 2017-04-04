using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    class NiRenderObject : NiAVObject
    {
        public int numMaterials;
        public List<int> materialExtraData;
        public List<String> materialNames;
        public int materialIndex;
        public bool materialNeedsUpdate;

        protected void loadRenderable(NIFFile file, BinaryReader ds)
        {
            loadAVObject(file, ds);
            numMaterials = ds.readInt();
            materialExtraData = new List<int>(numMaterials);
            materialNames = new List<String>(numMaterials);
            for (int i = 0; i < numMaterials; i++)
            {
                int matNameIndex = ds.readInt();
                String matName = file.getStringFromTable(matNameIndex);
                materialExtraData.Add(ds.readInt());
                materialNames.Add(matName);
            }
            materialIndex = ds.readInt();
            materialNeedsUpdate = ds.readUnsignedByte() > 0;

        }
    }
}
