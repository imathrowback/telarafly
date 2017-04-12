using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.NIF
{
    public class NiEvaluator : NIFObject
    {
        public int m_kAVObjectName;
        public int m_kCtlrID;
        public int m_kCtlrType;
        public int m_kEvaluatorID;
        public int m_kPropertyType;
        public int m_usLargeHashTableValue;
        public int m_usSmallHashTableValue;

        public override void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            base.parse(file, baseo, ds);

            this.m_kAVObjectName = ds.readInt();
            this.m_kPropertyType = ds.readInt();
            this.m_kCtlrType = ds.readInt();
            this.m_kCtlrID = ds.readInt();
            this.m_kEvaluatorID = ds.readInt();

            this.m_usSmallHashTableValue = ds.readUnsignedShort();
            this.m_usLargeHashTableValue = ds.readUnsignedShort();
        }

    }
    public class NiBSplineEvaluator : NiEvaluator
    {
        public int basisDataIndex;
        public float m_fEndTime;
        public float m_fStartTime;
        public int splineDataIndex;

        public override void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            base.parse(file, baseo, ds);
            this.m_fStartTime = ds.readFloat();
            this.m_fEndTime = ds.readFloat();
            this.splineDataIndex = ds.readInt();
            this.basisDataIndex = ds.readInt();
        }

    }

    public class NiBSplineTransformEvaluator : NiBSplineEvaluator
    {
        public Vector3 translate;
        public Quaternion rotate;
        public float scale;
        public int m_kTransCPHandle;
        public int m_kRotCPHandle;
        public int m_kScaleCPHandle;

        public override void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            base.parse(file, baseo, ds);
            this.translate = new Vector3(ds.readFloat(), ds.readFloat(), ds.readFloat());
            this.rotate = new Quaternion(ds.readFloat(), ds.readFloat(), ds.readFloat(), ds.readFloat());
            this.scale = ds.readFloat();

            // Handles into the NiBSplineData for access to control points
            this.m_kTransCPHandle = ds.readInt();
            this.m_kRotCPHandle = ds.readInt();
            this.m_kScaleCPHandle = ds.readInt();
          

        }
    }
    public class NiBSplineCompTransformEvaluator : NiBSplineTransformEvaluator
    {
        public enum COMP_
        {
                POSITION_OFFSET = 0,
                POSITION_RANGE,
                ROTATION_OFFSET,
                ROTATION_RANGE,
                SCALE_OFFSET,
                SCALE_RANGE,
                NUM_SCALARS
            };
        public float[]m_afCompScalars = new float[(int)COMP_.NUM_SCALARS];

        public override void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            base.parse(file, baseo, ds);
            for (int i = 0; i < m_afCompScalars.Length; i++)
                m_afCompScalars[i] = ds.readFloat();
        }
    }
}
