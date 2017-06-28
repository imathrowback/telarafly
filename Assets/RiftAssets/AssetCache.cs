using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Assets.RiftAssets
{
    class AssetCache
    {
        class Entry
        {
            internal string id;
            internal DateTime expires;
            internal byte[] data;
        }

        Dictionary<string, int> stats = new Dictionary<string, int>();

        int EXPIRE_MINUTES = 3;
        Timer timer;
        public AssetCache()
        {
           timer = new Timer(purgeExpired, null, 5000, Timeout.Infinite);
            stats["miss"] = 0;
            stats["hit"] = 0;
            stats["purged"] = 0;
        }

        SortedDictionary<string, Entry> entries = new SortedDictionary<string, Entry>();
        public byte[] GetOrAdd(string strID, Func<byte[]> getFunc)
        {
            lock (entries)
            {
                Entry entry;
                if (!entries.TryGetValue(strID, out entry))
                {
                    addStat("miss", 1);
                    entry = new RiftAssets.AssetCache.Entry();
                    entry.id = strID;
                    entry.data = getFunc.Invoke();
                    entries.Add(strID, entry);
                }
                else
                    addStat("hit", 1);
                entry.expires = DateTime.Now.AddMinutes(EXPIRE_MINUTES);
                return entry.data;
            }
        }

        void writeStats()
        {
            using (StreamWriter fs = File.CreateText("asset-cache.txt"))
            {
                foreach (string str in stats.Keys)
                {
                    fs.WriteLine(str + ":" + stats[str]);
                }
            }
        }
        /// <summary>
        ///  Purge expired entries
        /// </summary>
        void purgeExpired(Object state)
        {
            lock (entries)
            {
                foreach (Entry entry in entries.Values.ToArray())
                {
                    if (DateTime.Now > entry.expires)
                    {
                        entries.Remove(entry.id);
                        addStat("purged", 1);
                    }
                }
                writeStats();
            }
        }

        void addStat(string str, int count)
        {
            int value = 0;
            if (!stats.TryGetValue(str, out value))
                stats[str] = 0;
            stats[str] = stats[str] + count;
        }
    }
}
