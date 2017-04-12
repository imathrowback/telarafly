using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.NIF
{
    public class NiBSplineData : NIFObject
    {
        public uint controlPointCount;
        public float[] controlPoints;

        public uint compactControlPointCount;
        public short[] compactControlPoints;

        public int getCompactControlPointStart(int handle, int idx, int dim)
        {
            //Debug.Log("handle[" + handle + "], idx[" + idx + "], dim[" + dim + "]");
            return (idx * dim) + handle;
        }

        float[] decompressFloatArray(short[] sary, int startIndex, int numItems, float offset, float range)
        {
            //Debug.Log("startIndex:" + startIndex + " ary:" + sary.Length);
            float[] ary = new float[numItems];
            for (int i = 0; i < numItems; i++)
                ary[i] = (((float)sary[i+startIndex] / (float)short.MaxValue) * range) + offset;
            return ary;
        }
        
        public void getCompactValueDegree3(float time,  float[]  afPos, int dim, NiBSplineBasisData basisData, int handle, float offset, float halfRange)
        {
            int iMin, iMax;
            if (dim == 3)
                basisData.compute3(time,  out iMin,  out iMax);
            else
                basisData.computeNon3(time, out iMin, out iMax);

            int controlPointIdxStart = getCompactControlPointStart(handle, iMin, dim);

            int numItems = dim * 4;
            float[] source = decompressFloatArray(this.compactControlPoints, controlPointIdxStart, numItems, offset, halfRange);
            
            float basisValue = basisData.m_afValue[0];
            int srcIdx = 0;
            int j = 0;
            for (j = 0; j < dim; j++)
                afPos[j] = basisValue * source[srcIdx++];
            for (int i = iMin +1, iIndex =1; i <= iMax; i++,iIndex++)
            {
                basisValue = basisData.m_afValue[iIndex];
                for (j = 0; j < dim; j++)
                    afPos[j] += basisValue * source[srcIdx++];
            }
        }

        public override void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            base.parse(file, baseo, ds);

            this.controlPointCount = ds.readUInt();
            this.controlPoints = new float[this.controlPointCount];

            for (int i = 0; i < controlPointCount; i++)
                controlPoints[i] = ds.readFloat();

            this.compactControlPointCount = ds.readUInt();
            this.compactControlPoints = new short[compactControlPointCount];
            for (int i = 0; i < compactControlPointCount; i++)
                compactControlPoints[i] = ds.ReadInt16();

        }
    }
}
