using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    class NiTexturingProperty : NiProperty
    {
        public int texPropFlags;
        public List<NifTexMap> texList;
        public List<NifTexMap> shaderMapList;

        public override void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            base.parse(file, baseo, ds);
            base.loadObjectNET(file, ds);

            texPropFlags = ds.readUnsignedShort();

            int texListSize = ds.readInt();
            texList = new List<NifTexMap>(texListSize);
            for (int i = 0; i < texListSize; i++)
            {
                NifTexMap tex = null;
                bool hasMap = ds.readUnsignedByte() > 0;
                if (hasMap)
                {
                    tex = new NifTexMap();
                    tex.parse(file, baseo, ds);
                    if (i == 5)
                    {
                        tex.bumpLumaScale = ds.readFloat();
                        tex.bumpLumaOffset = ds.readFloat();
                        tex.bumpMap = new Point4f(ds.readFloat(), ds.readFloat(), ds.readFloat(), ds.readFloat());
                    }
                    else if (i == 7)
                    {
                        tex.offsetMapOfs = ds.readFloat();
                    }
                }
                else
                    tex = null;
                texList.Add(tex);
            }

            int shaderMapListSize = ds.readInt();
            shaderMapList = new List<NifTexMap>(shaderMapListSize);
            for (int i = 0; i < shaderMapListSize; i++)
            {
                NifTexMap tex = null;
                bool hasMap = ds.readUnsignedByte() > 0;
                if (hasMap)
                {
                    tex = new NifTexMap();
                    tex.parse(file, baseo, ds);
                    tex.uniqueID = ds.readInt();
                }
                shaderMapList.Add(tex);
            }

        }
    }
}
