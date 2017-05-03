using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Assets.Database
{
    [Serializable()]
    public class DB
    {
        public string dbchecksum;
        
        private Dictionary<long, Dictionary<long, entry>> data = new Dictionary<long, Dictionary<long, entry>>();


        public List<entry> getEntries()
        {
            List<entry> entries = new List<entry>();
            List<Dictionary<long, entry>> dict = data.Values.ToList();
            foreach (Dictionary<long, entry> d in dict)
                entries.AddRange(d.Values.ToList());
            return entries;
        }
        internal void Add(entry e)
        {
            if (!data.ContainsKey(e.id))
                data[e.id] = new Dictionary<long, entry>();
            data[e.id][e.key] = e;
        }


        public IEnumerable<entry> getEntriesForID(long datasetid)
        {
            return data[datasetid].Values;
        }

        public entry getEntry(long datasetid, long key)
        {
            Dictionary<long, entry> ds = data[datasetid];
            if (!ds.ContainsKey(key))
                return null;
            return ds[key];
        }

        public byte[] getData(long id, long key)
        {
            return getEntry(id, key).decompressedData;
        }

        internal bool hasEntry(long id, long key)
        {
            return getEntry(id, key) != null;
        }

    }
    [Serializable()]
    public class entry
    {
        public long key;
        public long id;
        public string name;
        public byte[] decompressedData;
    }
}
