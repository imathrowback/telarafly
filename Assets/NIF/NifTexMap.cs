using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    public class NifTexMap
    {
        public float bumpLumaScale;
        public float bumpLumaOffset;
        public Point4f bumpMap;
        public float offsetMapOfs;
        public int sourceTexLinkID;
        public int flags;
        public int maxAniso;
        public bool hasTransform;
        public Point3f translation;
        public float scale;
        public float rotate;
        public int method;
        public Point2f center;
        public int uniqueID;

        public void parse( NIFFile file,  NIFObject baseo,  BinaryReader ds)
        {
            sourceTexLinkID = ds.readInt();
            flags = ds.readUnsignedShort();
            maxAniso = ds.readUnsignedShort();
            hasTransform = ds.readUnsignedByte() > 0;
		    if (hasTransform)

            {
                    translation = new Point3f(ds.readFloat(), ds.readFloat(), ds.readFloat());
                    scale = ds.readFloat();
                    rotate = ds.readFloat();
                    method = ds.readInt();
                    center = new Point2f(ds.readFloat(), ds.readFloat());
                }
            }
    }
}
