using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.NIF
{
    public class NiMeshModifier : NIFObject
    {
        public List<int> m_kSubmitPoints = new List<int>();
        public List<int> m_kCompletePoints = new List<int>();
        public override void parse(NIFFile file, NIFObject baseo, BinaryReader ds)
        {
            base.parse(file, baseo, ds);
            uint uiSyncCount = ds.readUInt();
            for (int i = 0; i < uiSyncCount; i++)
                m_kSubmitPoints.Add(ds.readUnsignedShort());
            uiSyncCount = ds.readUInt();
            for (int i = 0; i < uiSyncCount; i++)
                m_kCompletePoints.Add(ds.readUnsignedShort());
        }
    }
}
