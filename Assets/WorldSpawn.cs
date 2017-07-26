using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets
{
    public class WorldSpawn : IComparable<WorldSpawn>
    {
        public string worldName;
        public string spawnName;
        public Vector3 pos;
        public float angle;

        public string imagePath { get; internal set; }

        public WorldSpawn(string worldName, string spawnName, Vector3 pos, float angle)
        {
            this.worldName = worldName;
            this.spawnName = spawnName;
            this.pos = pos;
            this.angle = angle;
        }

        public int CompareTo(WorldSpawn other)
        {
            int compare = worldName.CompareTo(other.worldName);
            if (compare == 0)
                return spawnName.CompareTo(other.spawnName);
            return compare;
        }
    }
}
