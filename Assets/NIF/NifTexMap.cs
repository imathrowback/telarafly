using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    class NifTexMap
    {
        public float bumpLumaScale;
        public float bumpLumaOffset;
        public Point4f bumpMap;
        public float offsetMapOfs;
        public int sourceTexLinkID;
        private int flags;
        private int maxAniso;
        private bool hasTransform;
        private Point3f translation;
        private float scale;
        private float rotate;
        private int method;
        private Point2f center;
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
