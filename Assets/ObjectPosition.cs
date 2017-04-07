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
}
