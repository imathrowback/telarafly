using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets
{
    public class ObjectPosition
    {
        public string nifFile;
        public Vector3 min;
        public Quaternion qut;
        public Vector3 max;
        public float scale;

        public ObjectPosition(string nifFile, Vector3 min, Quaternion qut, Vector3 max, float scale)
        {
            this.nifFile = nifFile;
            this.min = min;
            this.qut = qut;
            this.max = max;
            this.scale = scale;
        }
    }

    public class LightPosition : ObjectPosition
    {
        public float range;
        public float r;
        public float g;
        public float b;
        public LightPosition(float range, float r, float g, float b, Vector3 min, Quaternion qut, Vector3 max, float scale) : base(null, min, qut, max, scale)
        {
            this.range = range;
            this.r = r;
            this.g = g;
            this.b = b;
        }
    }
}
