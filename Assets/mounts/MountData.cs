using Assets.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.mounts
{
    public class MountData
    {
        public string name;
        public string nifName;
        public Vector3 xOffset;

    }

    public class FMounts
    {
        static List<MountData> getMounts()
        {
            return new List<MountData>();
            //DBInst.inst;
        }
    }
}
