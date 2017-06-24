using Assets.DatParser;
using Assets.RiftAssets;
using CGS;
using Ionic.Zlib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Database
{
    public delegate void ProgressCallback(string message);
    public delegate void LoadedCallback(DB db);
    public static class DBInst
    {
        private static System.Threading.Thread loadThread;
        static object lockObj = new object();

        public static CObject toObj(this DB db, long ds, long key)
        {
            entry e = db.getEntry(ds, key);
            MemoryStream str = new MemoryStream(e.decompressedData);
            return Parser.processStreamObject(str);
        }

        public static bool loaded = false;
        public static bool loading {  get { return loadThread.IsAlive; } }
        static private EnglishLang langdb;
        static private DB db;
        public static DB inst   { get {
                while (db == null) ;
                lock (lockObj)
                {
                    return db;
                }
            }
        }

        public static EnglishLang lang_inst
        {
            get
            {
                lock (lockObj)
                {
                    return langdb;
                }
            }
        }


        public static event ProgressCallback progress = delegate { };
        public static event LoadedCallback isloadedCallback = delegate { };

        /**
         * If the db is loaded, call the callback immediately, otherwise, register the callback
         */ 
        public static void loadOrCallback(LoadedCallback loadCallback)
        {
            if (db != null)
                loadCallback.Invoke(db);
            else
                isloadedCallback += loadCallback;
        }

        static DBInst()
        {
            loadThread = new System.Threading.Thread(new System.Threading.ThreadStart(loadDatabase_));
            loadThread.Start();
        }
        
        private static void loadDatabase_()
        {
            lock (lockObj)
            {
                AssetDatabase adb = AssetDatabaseInst.DB;
                AssetEntry ae = adb.getEntryForFileName("telara.db");
                string expectedChecksum = BitConverter.ToString(ae.hash);
                DB db = readDB(expectedChecksum, (s) => { progress.Invoke("[Phase 1 of 2]" + s); });
                if (db == null)
                {
                    create(AssetDatabaseInst.ManifestFile, AssetDatabaseInst.AssetsDirectory);
                    db = readDB(expectedChecksum, (s) => { progress.Invoke("[Phase 1.1 of 2]" + s); });
                }

                langdb = new EnglishLang(adb, (s) => { progress.Invoke("[Phase 2 of 2]" + s); });

                DBInst.db = db;
                if (db != null)
                {
                    loaded = true;
                    isloadedCallback.Invoke(db);
                    progress.Invoke("");
                }

            }
        }

        private static DB readDB(string expectedChecksum, Action<String> progress)
        {
            DB db = new DB();
            try
            {
                if (DBInst.db != null)
                    return DBInst.db;
                using (FileStream fs = new FileStream("dat.xmlz", FileMode.Open))
                {
                    using (ProgressStream ps = new ProgressStream(fs))
                    {
                        long total = fs.Length;
                        ps.BytesRead += (s, a) => progress.Invoke("Loading Database: " + (int)(((float)a.StreamPosition / (float)total) * 100.0) + " %");

                        using (DeflateStream ds = new DeflateStream(ps, CompressionMode.Decompress))
                        {
                            long pos = fs.Position;
                            //Debug.Log("begin read");
                            using (BinaryReader reader = new BinaryReader(ds))
                            {
                                db.dbchecksum = reader.ReadString();
                                if (!expectedChecksum.Equals(db.dbchecksum))
                                {
                                    db = null;
                                    UnityEngine.Debug.Log("Checksum in file doesn't match file from assets");
                                    return null;
                                }
                                else
                                    UnityEngine.Debug.Log("Found existing database, let us use it!");

                                int count = reader.ReadInt32();
                                for (int i = 0; i < count; i++)
                                {
                                    entry e = new entry();
                                    e.id = reader.ReadInt64();
                                    e.key = reader.ReadInt64();
                                    e.name = reader.ReadString();
                                    e.decompressedData = new byte[reader.ReadInt32()];
                                    reader.Read(e.decompressedData, 0, e.decompressedData.Length);

                                    db.Add(e);
                                }
                               
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log("Unable to read existing database so we will recreate it:" + ex);
                db = null;
                return null;
            }
            finally
            {
            }
            return db;
        }

        internal static void create(string assetManifest, string assetDir)
        {
            System.Diagnostics.Process pr;
            string file = @"decomp\tdbdecomp.exe";
            pr = new System.Diagnostics.Process();
            pr.StartInfo.FileName = file;
            pr.StartInfo.Arguments = "\"" + assetManifest + "\" \"" + assetDir + "\"";
            pr.Start();
            pr.WaitForExit();
        }
    }
}
