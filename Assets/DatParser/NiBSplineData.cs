using Assets.NIF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.DatParser
{
    public class NiBSplineData : NIFObject
    {
        private float[] floatControlPoints;
        private uint numFloatControlPoints;
        private uint numShortControlPoints;
        private int[] shortControlPoints;

        public override void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            base.parse(file, baseo, ds);

            this.numFloatControlPoints = ds.readUInt();
            this.floatControlPoints = new float[numFloatControlPoints];
            for (int i = 0; i < numFloatControlPoints; i++)
                floatControlPoints[i] = ds.readFloat();

            this.numShortControlPoints = ds.readUInt();
            this.shortControlPoints = new int[numShortControlPoints];
            for (int i = 0; i < numShortControlPoints; i++)
                shortControlPoints[i] = ds.readUnsignedShort();

        }
    }
}
