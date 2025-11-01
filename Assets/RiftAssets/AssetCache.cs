using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Assets.RiftAssets
{
    /** Cache asset data for a short period */
    class AssetCache
    {
        static AssetCache _inst;

        public static AssetCache inst { get
            {
                if (_inst == null)
                    _inst = new AssetCache();
                return _inst;
            }
        }
        class Entry
        {
            internal string id;
            internal DateTime expires;
            private byte[] data;

            public byte[] getData()
            {
                lock (id)
                {
                    if (data == null)
                        data = getFunc.Invoke();
                    return data;
                }
            }
            internal Func<byte[]> getFunc;
        }

        Dictionary<string, int> stats = new Dictionary<string, int>();

        int EXPIRE_SECONDS = 30;
        Timer timer;
        int expiredLast = 0;

        void stopTimer()
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }
        void startTimer()
        {
            try
            {
                if (timer == null)
                    timer = new Timer(purgeExpired, null, 5000, 5000);

            }
            catch (Exception ex)
            {

            }
        }
        private AssetCache()
        {
            stats["miss"] = 0;
            stats["hit"] = 0;
            stats["purged"] = 0;
        }

        SortedDictionary<string, Entry> entries = new SortedDictionary<string, Entry>();
        public byte[] GetOrAdd(string strID, Func<byte[]> getFunc)
        {
            Entry entry;

            lock (entries)
            {
                if (!entries.TryGetValue(strID, out entry))
                {
                    addStat("miss", 1);
                    entry = new RiftAssets.AssetCache.Entry();
                    entry.id = strID;
                    entry.getFunc = getFunc;
                    entries.Add(strID, entry);
                }
                else
                {
                    //Debug.Log("[" + strID + "] cache hit");
                    addStat("hit", 1);
                }
                entry.expires = DateTime.UtcNow.AddSeconds(EXPIRE_SECONDS);
                startTimer();
            }
            return entry.getData();
        }

        void writeStats()
        {
            if (true)
            {
               //Debug.Log("write cache stats");

                using (StreamWriter fs = File.CreateText("asset-cache.txt"))
                {
                    foreach (string str in stats.Keys)
                    {
                        fs.WriteLine(str + ":" + stats[str]);
                    }
                }
            }
        }
        /// <summary>
        ///  Purge expired entries
        /// </summary>
        void purgeExpired(System.Object state)
        {
            lock (entries)
            {
                foreach (Entry entry in entries.Values.ToArray())
                {
                    if (DateTime.UtcNow > entry.expires)
                    {
                        entries.Remove(entry.id);
                        addStat("purged", 1);
                    }
                }
                writeStats();
                if (entries.Count == 0)
                    stopTimer();
            }
        }

        void addStat(string str, int count)
        {
            int value = 0;
           // Debug.Log("add stat [" + str + "] + " + count);
            if (!stats.TryGetValue(str, out value))
                stats[str] = 0;
            stats[str] = stats[str] + count;
            //Debug.Log("-- final stat [" + str + "] = " + stats[str]);
        }
    }
}
