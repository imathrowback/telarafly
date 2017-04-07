using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets
{
    public static class GameWorld
    {
        static List<ObjectPosition> objects = new List<ObjectPosition>(50000);

        public static WorldSpawn initialSpawn { get; set; }

        internal static void Add(ObjectPosition objectPosition)
        {
            objects.Add(objectPosition);
        }
        internal static List<ObjectPosition> getObjects()
        {
            return objects;
        }
        internal static void Clear()
        {
            objects.Clear();
        }
    }
}
