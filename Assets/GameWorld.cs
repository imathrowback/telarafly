using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets
{
    public static class GameWorld
    {
        //static List<ObjectPosition> objects = new List<ObjectPosition>(50000);
        public static int minX = 0;
        public static int minY = 0;
        public static int maxX = 0;
        public static int maxY = 0;
        public static string worldName;
        static List<WorldSpawn> spawns = new List<WorldSpawn>();
        public static WorldSpawn initialSpawn { get; set; }
        public static bool useColliders { get; internal set; }

        public static List<WorldSpawn> getSpawns() { return spawns;  }
      
        internal static void Clear()
        {
            
            spawns.Clear();
        }

        internal static void AddSpawns(WorldSpawn s)
        {
            spawns.Add(s);
        }
    }
}
