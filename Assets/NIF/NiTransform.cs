using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.NIF
{
    public class NITransform
    {
        public Matrix4f matrix;
        public float scale;
        private Point3f translation;

        public static NITransform parse(BinaryReader ds)
        {
            NITransform trans = new NITransform();
            Point3f _1 = new Point3f(ds.readFloat(), ds.readFloat(), ds.readFloat());
            Point3f _2 = new Point3f(ds.readFloat(), ds.readFloat(), ds.readFloat());
            Point3f _3 = new Point3f(ds.readFloat(), ds.readFloat(), ds.readFloat());
            Point3f _4 = new Point3f(ds.readFloat(), ds.readFloat(), ds.readFloat());

            trans.matrix = new Matrix4f(_1.x, _2.x, _3.x, 0,
                _1.y, _2.y, _3.y, 0,
                _1.z, _2.z, _3.z, 0,
                _4.x, _4.y, _4.z, 1
                );
            trans.scale = ds.readFloat();
            return trans;
        }
    }
}
