using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    class Point3f
    {
        public float x, y, z;
        public Point3f(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override string ToString()
        {
            return "[" + x + "," + y + "," + z + "]";
        }
    }

    class Point2f
    {
        public float x, y;
        public Point2f(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
    }


    class Point4f
    {
        public float x, y, z,w;
        public Point4f(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }

}
