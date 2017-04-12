using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets
{
    public static class GameWorld
    {
        static List<ObjectPosition> objects = new List<ObjectPosition>(50000);
        static List<WorldSpawn> spawns = new List<WorldSpawn>();
        public static WorldSpawn initialSpawn { get; set; }
        public static bool useColliders { get; internal set; }

        public static List<WorldSpawn> getSpawns() { return spawns;  }
        internal static void Add(ObjectPosition objectPosition)
        {
            lock (objects)
            {
                objects.Add(objectPosition);
            }
        }
        internal static List<ObjectPosition> getObjects()
        {
            return objects;
        }
        internal static void Clear()
        {
            objects.Clear();
            spawns.Clear();
        }

        internal static void AddSpawns(WorldSpawn s)
        {
            spawns.Add(s);
        }
    }
}
