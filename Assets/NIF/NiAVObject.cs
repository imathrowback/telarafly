using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.NIF
{
   public class NiAVObject : NiObjectNET
    {
        public Matrix4f matrix;
        public float scale;
        public List<int> nodePropertyIDs;
        public bool isBone;
        public Point3f translation;

        protected void loadAVObject(NIFFile file,  BinaryReader ds)
        {
            loadObjectNET(file, ds);

      
            int flags = ds.readUnsignedShort();

            // if hack readUShort
            translation = new Point3f(ds.readFloat(), ds.readFloat(), ds.readFloat());
            matrix = new Matrix4f(ds.readFloat(), ds.readFloat(), ds.readFloat(), 0,
				    ds.readFloat(), ds.readFloat(), ds.readFloat(), 0,
				    ds.readFloat(), ds.readFloat(), ds.readFloat(), 0,
				    translation.x, translation.y, translation.z, 0);
		    scale = ds.readFloat();
            nodePropertyIDs = loadLinkIDs(ds);

            loadLinkID(ds); // collision node?
            isBone = true;
	    }
    }
}
